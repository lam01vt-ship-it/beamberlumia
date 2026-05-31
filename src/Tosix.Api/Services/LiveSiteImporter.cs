using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Tosix.Api.Data;
using Tosix.Api.Entities;

namespace Tosix.Api.Services;

public sealed partial class LiveSiteImporter(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    SeedImageResolver imageResolver,
    ILogger<LiveSiteImporter> logger)
{
    private const string BaseUrl = "https://tosixdecor.com.vn";

    private static readonly HashSet<string> SkippedCategorySlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "den-trang-tri",
    };

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var http = httpClientFactory.CreateClient(nameof(LiveSiteImporter));
        http.Timeout = TimeSpan.FromMinutes(5);

        logger.LogInformation("Đang tải dữ liệu từ {BaseUrl}…", BaseUrl);

        var wcCategories = await FetchAllPagesAsync<WcCategory>(
            http, $"{BaseUrl}/wp-json/wc/store/v1/products/categories?per_page=100", cancellationToken);
        var wcProducts = await FetchAllPagesAsync<WcProduct>(
            http, $"{BaseUrl}/wp-json/wc/store/v1/products?per_page=100", cancellationToken);

        logger.LogInformation("Tìm thấy {CategoryCount} danh mục, {ProductCount} sản phẩm.", wcCategories.Count, wcProducts.Count);

        await ClearCatalogAsync(cancellationToken);

        var categoryMap = await ImportCategoriesAsync(wcCategories, wcProducts, http, cancellationToken);
        await ImportProductsAsync(wcProducts, categoryMap, http, cancellationToken);
        await ImportBannersAsync(http, cancellationToken);
        await ImportHomepageGalleryAsync(http, cancellationToken);
        await ImportNewArrivalFlagsAsync(wcProducts, cancellationToken);

        logger.LogInformation("Import hoàn tất.");
    }

    public async Task ImportGalleriesAsync(CancellationToken cancellationToken = default)
    {
        var http = httpClientFactory.CreateClient(nameof(LiveSiteImporter));
        http.Timeout = TimeSpan.FromMinutes(5);

        logger.LogInformation("Đang đồng bộ gallery feedback & đánh giá từ {BaseUrl}…", BaseUrl);

        db.FeedbackImages.RemoveRange(await db.FeedbackImages.ToListAsync(cancellationToken));
        db.CustomerReviews.RemoveRange(await db.CustomerReviews.ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);

        await ImportHomepageGalleryAsync(http, cancellationToken);
        logger.LogInformation("Đồng bộ gallery hoàn tất.");
    }

    private async Task ClearCatalogAsync(CancellationToken cancellationToken)
    {
        db.ProductImages.RemoveRange(await db.ProductImages.ToListAsync(cancellationToken));
        db.Products.RemoveRange(await db.Products.ToListAsync(cancellationToken));
        db.Banners.RemoveRange(await db.Banners.ToListAsync(cancellationToken));
        db.FeedbackImages.RemoveRange(await db.FeedbackImages.ToListAsync(cancellationToken));
        db.CustomerReviews.RemoveRange(await db.CustomerReviews.ToListAsync(cancellationToken));
        db.ProductCategories.RemoveRange(await db.ProductCategories.ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, ProductCategory>> ImportCategoriesAsync(
        IReadOnlyList<WcCategory> wcCategories,
        IReadOnlyList<WcProduct> wcProducts,
        HttpClient http,
        CancellationToken cancellationToken)
    {
        var selected = wcCategories
            .Where(c => c.Count > 0 && !SkippedCategorySlugs.Contains(c.Slug))
            .OrderBy(c => c.Parent)
            .ThenBy(c => c.Name)
            .ToList();

        var map = new Dictionary<string, ProductCategory>(StringComparer.OrdinalIgnoreCase);
        var order = 1;

        foreach (var wc in selected)
        {
            var sampleProduct = wcProducts.FirstOrDefault(p =>
                p.Categories.Any(c => c.Slug.Equals(wc.Slug, StringComparison.OrdinalIgnoreCase))
                && p.Images.Count > 0);

            var imageUrl = sampleProduct?.Images[0].Src;
            var imagePath = imageUrl is null
                ? null
                : await imageResolver.ResolveOrDownloadAsync(imageUrl, http, cancellationToken);

            var entity = new ProductCategory
            {
                Id = Guid.NewGuid(),
                Name = wc.Name.Trim(),
                Slug = wc.Slug.Trim().ToLowerInvariant(),
                ImagePath = imagePath,
                SortOrder = order++,
                IsActive = true,
            };

            db.ProductCategories.Add(entity);
            map[entity.Slug] = entity;
        }

        await db.SaveChangesAsync(cancellationToken);
        return map;
    }

    private async Task ImportProductsAsync(
        IReadOnlyList<WcProduct> wcProducts,
        IReadOnlyDictionary<string, ProductCategory> categoryMap,
        HttpClient http,
        CancellationToken cancellationToken)
    {
        var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 1;

        foreach (var wc in wcProducts)
        {
            var categorySlug = PickCategorySlug(wc);
            if (categorySlug is null || !categoryMap.TryGetValue(categorySlug, out var category))
                continue;

            var code = BuildUniqueCode(wc, usedCodes);
            var price = ParsePrice(wc);
            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Code = code,
                Name = wc.Name.Trim(),
                Price = price,
                IsNew = wc.Categories.Any(c => c.Slug.Equals("new-arrival", StringComparison.OrdinalIgnoreCase)),
                SortOrder = order++,
                IsActive = true,
            };

            var imageOrder = 0;
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var wcImage in wc.Images)
            {
                var imagePath = await imageResolver.ResolveOrDownloadAsync(wcImage.Src, http, cancellationToken);
                if (string.IsNullOrWhiteSpace(imagePath) || !seenPaths.Add(imagePath))
                    continue;

                product.Images.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ImagePath = imagePath,
                    SortOrder = imageOrder++,
                });
            }

            product.ImagePath = product.Images.FirstOrDefault()?.ImagePath;
            db.Products.Add(product);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportBannersAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var order = 1;
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in new[] { "1.jpg", "12.jpg", "3-1.jpg" })
        {
            var path = imageResolver.ResolveLocalSeedFile(name);
            if (path is null || !added.Add(path)) continue;
            db.Banners.Add(new Banner { Id = Guid.NewGuid(), ImagePath = path, SortOrder = order++, IsActive = true });
        }

        if (!added.Any())
        {
            var html = await http.GetStringAsync(BaseUrl, cancellationToken);
            var urls = ExtractPageUploadUrls(html).Take(5).ToList();
            foreach (var url in urls)
            {
                var path = await imageResolver.ResolveOrDownloadAsync(url, http, cancellationToken);
                if (path is null || !added.Add(path)) continue;
                db.Banners.Add(new Banner { Id = Guid.NewGuid(), ImagePath = path, SortOrder = order++, IsActive = true });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportHomepageGalleryAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var html = await http.GetStringAsync(BaseUrl, cancellationToken);
        var feedbackUrls = ExtractSliderSectionImages(html, "HÌNH ẢNH FEEDBACK", "Đánh giá của khách hàng");
        var reviewUrls = ExtractSliderSectionImages(html, "Đánh giá của khách hàng", "THÔNG TIN LIÊN HỆ");

        logger.LogInformation("Tìm thấy {FeedbackCount} ảnh feedback, {ReviewCount} ảnh đánh giá.", feedbackUrls.Count, reviewUrls.Count);

        var feedbackOrder = 1;
        foreach (var url in feedbackUrls)
        {
            var path = await imageResolver.ResolveOrDownloadAsync(url, http, cancellationToken);
            if (path is null) continue;
            db.FeedbackImages.Add(new FeedbackImage
            {
                Id = Guid.NewGuid(),
                ImagePath = path,
                SortOrder = feedbackOrder++,
                IsActive = true,
            });
        }

        var reviewOrder = 1;
        foreach (var url in reviewUrls)
        {
            var path = await imageResolver.ResolveOrDownloadAsync(url, http, cancellationToken);
            if (path is null) continue;
            db.CustomerReviews.Add(new CustomerReview
            {
                Id = Guid.NewGuid(),
                ImagePath = path,
                SortOrder = reviewOrder++,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<string> ExtractSliderSectionImages(string html, string startMarker, string endMarker)
    {
        var start = html.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return [];

        var end = html.IndexOf(endMarker, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            end = html.Length;

        var section = html[start..end];
        var sliderStart = section.IndexOf("slider-wrapper", StringComparison.OrdinalIgnoreCase);
        if (sliderStart < 0)
            return [];

        var sliderHtml = section[sliderStart..];
        var urls = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in ImgTagRegex().Matches(sliderHtml))
        {
            var url = PickBestImageFromTag(match.Value);
            if (url is null || !IsGalleryImage(url) || !seen.Add(url))
                continue;

            urls.Add(url);
        }

        return urls;
    }

    private static bool IsGalleryImage(string url)
    {
        if (url.Contains("fb03.png", StringComparison.OrdinalIgnoreCase))
            return false;
        if (url.Contains("den-san-den", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string? PickBestImageFromTag(string imgTag)
    {
        var srcsetMatch = SrcsetRegex().Match(imgTag);
        if (srcsetMatch.Success)
        {
            var best = PickLargestFromSrcset(srcsetMatch.Groups[1].Value);
            if (best is not null)
                return best;
        }

        var srcMatch = SrcRegex().Match(imgTag);
        return srcMatch.Success ? srcMatch.Groups[1].Value : null;
    }

    private static string? PickLargestFromSrcset(string srcset)
    {
        string? bestUrl = null;
        var bestWidth = 0;

        foreach (var part in srcset.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var space = part.LastIndexOf(' ');
            if (space <= 0)
                continue;

            var url = part[..space].Trim();
            var descriptor = part[(space + 1)..].Trim();
            if (!descriptor.EndsWith("w", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(descriptor[..^1], out var width))
                continue;

            if (width > bestWidth)
            {
                bestWidth = width;
                bestUrl = url;
            }
        }

        return bestUrl;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<img\b[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex ImgTagRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"srcset=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex SrcsetRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"\bsrc=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
    private static partial System.Text.RegularExpressions.Regex SrcRegex();

    private static IEnumerable<string> ExtractPageUploadUrls(string html)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            html,
            @"https://tosixdecor\.com\.vn/wp-content/uploads/[^\s""']+\.(?:jpg|jpeg|png|webp)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var url = match.Value;
            if (url.Contains("-50x50.", StringComparison.OrdinalIgnoreCase)
                || url.Contains("-100x100.", StringComparison.OrdinalIgnoreCase)
                || url.Contains("-150x150.", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return url;
        }
    }

    private async Task ImportNewArrivalFlagsAsync(
        IReadOnlyList<WcProduct> wcProducts,
        CancellationToken cancellationToken)
    {
        var newCodes = wcProducts
            .Where(p => p.Categories.Any(c => c.Slug.Equals("new-arrival", StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var products = await db.Products.ToListAsync(cancellationToken);
        foreach (var product in products)
        {
            if (newCodes.Contains(product.Name) || newCodes.Contains(product.Code))
                product.IsNew = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? PickCategorySlug(WcProduct product)
    {
        var slugs = product.Categories
            .Select(c => c.Slug)
            .Where(s => !SkippedCategorySlugs.Contains(s) && !s.Equals("new-arrival", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return slugs.FirstOrDefault();
    }

    private static string BuildUniqueCode(WcProduct product, ISet<string> usedCodes)
    {
        var baseCode = string.IsNullOrWhiteSpace(product.Slug) ? product.Name.Trim() : product.Slug.Trim();
        baseCode = baseCode.ToUpperInvariant();

        if (usedCodes.Add(baseCode))
            return baseCode;

        var i = 2;
        while (!usedCodes.Add($"{baseCode}-{i}"))
            i++;

        return $"{baseCode}-{i}";
    }

    private static decimal ParsePrice(WcProduct product)
    {
        if (decimal.TryParse(product.Prices?.Price, out var price))
            return price;
        return 0;
    }

    private static async Task<List<T>> FetchAllPagesAsync<T>(HttpClient http, string url, CancellationToken cancellationToken)
    {
        var items = new List<T>();
        var page = 1;

        while (true)
        {
            var pageUrl = url.Contains('?') ? $"{url}&page={page}" : $"{url}?page={page}";
            var batch = await http.GetFromJsonAsync<List<T>>(pageUrl, cancellationToken);
            if (batch is null || batch.Count == 0)
                break;

            items.AddRange(batch);
            if (batch.Count < 100)
                break;

            page++;
        }

        return items;
    }

    private sealed class WcCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("parent")]
        public int Parent { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    private sealed class WcProduct
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public List<WcImage> Images { get; set; } = [];

        [JsonPropertyName("categories")]
        public List<WcCategoryRef> Categories { get; set; } = [];

        [JsonPropertyName("prices")]
        public WcPrices? Prices { get; set; }
    }

    private sealed class WcImage
    {
        [JsonPropertyName("src")]
        public string Src { get; set; } = string.Empty;
    }

    private sealed class WcCategoryRef
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;
    }

    private sealed class WcPrices
    {
        [JsonPropertyName("price")]
        public string? Price { get; set; }
    }
}

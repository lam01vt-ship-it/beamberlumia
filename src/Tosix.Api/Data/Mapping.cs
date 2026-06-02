using Tosix.Api.Contracts;
using Tosix.Api.Entities;

namespace Tosix.Api.Data;

public static class Mapping
{
    public static SiteSettingDto ToDto(this SiteSetting s) =>
        new(s.Id, s.CompanyName, s.TaxCode, s.Address, s.Email, s.PhonePrimary, s.PhoneSecondary,
            s.FacebookUrl, s.ZaloUrl, s.ZaloQrImagePath, s.SiteTitle, s.SiteTagline,
            s.HeroEyebrow, s.LogoSubtitle,
            s.Trust1Title, s.Trust1Text, s.Trust2Title, s.Trust2Text, s.Trust3Title, s.Trust3Text,
            s.PolicyContent);

    public static CategoryDto ToDto(this ProductCategory c, int productCount = 0) =>
        new(c.Id, c.Name, c.Slug, c.ImagePath, c.SortOrder, c.IsActive, productCount);

    public static ProductDto ToDto(this Product p) =>
        new(p.Id, p.CategoryId, p.Category.Name, p.Code, p.Name, p.Price, p.PriceMax, p.ImagePath,
            p.IsNew, p.IsFeatured, p.IsInStock, p.IsOrder, p.IsUpdating, p.SortOrder, p.IsActive, p.Images.Count);

    public static ProductDto ToDto(this Product p, int imageCount) =>
        new(p.Id, p.CategoryId, p.Category.Name, p.Code, p.Name, p.Price, p.PriceMax, p.ImagePath,
            p.IsNew, p.IsFeatured, p.IsInStock, p.IsOrder, p.IsUpdating, p.SortOrder, p.IsActive, imageCount);

    public static AdminProductDetailDto ToAdminDetailDto(this Product p) =>
        new(p.ToDto(p.Images.Count), ProductImageHelper.GetGalleryPaths(p));

    public static ProductDetailDto ToDetailDto(this Product p, IReadOnlyList<ProductDto> relatedProducts)
    {
        var gallery = p.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ImagePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (gallery.Count == 0 && !string.IsNullOrWhiteSpace(p.ImagePath))
            gallery.Add(p.ImagePath);

        return new ProductDetailDto(p.ToDto(), gallery, relatedProducts);
    }

    public static BannerDto ToDto(this Banner b) =>
        new(b.Id, b.ImagePath, b.LinkUrl, b.SortOrder, b.IsActive);

    public static FeedbackImageDto ToDto(this FeedbackImage f) =>
        new(f.Id, f.ImagePath, f.Caption, f.SortOrder, f.IsActive);

    public static CustomerReviewDto ToDto(this CustomerReview r) =>
        new(r.Id, r.ImagePath, r.SortOrder, r.IsActive);
}

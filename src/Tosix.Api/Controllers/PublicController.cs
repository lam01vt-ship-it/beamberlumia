using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tosix.Api.Contracts;
using Tosix.Api.Data;

namespace Tosix.Api.Controllers;

[ApiController]
[Route("api/public")]
public sealed class PublicController(AppDbContext db) : ControllerBase
{
    [HttpGet("home")]
    public async Task<ActionResult<HomePageDto>> Home(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
            return NotFound();

        var banners = await db.Banners.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        var categories = await db.ProductCategories.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(c => c.ToDto(c.Products.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);

        var newProducts = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsNew)
            .OrderBy(p => p.SortOrder)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        var feedback = await db.FeedbackImages.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        var reviews = await db.CustomerReviews.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(new HomePageDto(
            settings.ToDto(),
            banners,
            categories,
            newProducts,
            feedback,
            reviews));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories(CancellationToken cancellationToken)
    {
        var items = await db.ProductCategories.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(c => c.ToDto(c.Products.Count(p => p.IsActive)))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("categories/{slug}/products")]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> ProductsByCategory(
        string slug,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var category = await db.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive, cancellationToken);
        if (category is null)
            return NotFound();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);

        var query = db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.CategoryId == category.Id && p.IsActive)
            .ApplySearch(q);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(ToPaged(items, page, pageSize, totalCount));
    }

    [HttpGet("products")]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> AllProducts(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 48);

        var query = db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ApplySearch(q);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(ToPaged(items, page, pageSize, totalCount));
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ProductDetailDto>> ProductDetail(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);

        if (product is null)
            return NotFound();

        var related = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderBy(p => p.SortOrder)
            .Take(8)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return Ok(product.ToDetailDto(related));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<SiteSettingDto>> Settings(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings is null ? NotFound() : Ok(settings.ToDto());
    }

    private static PagedResultDto<T> ToPaged<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
        new(items, page, pageSize, totalCount, totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tosix.Api.Contracts;
using Tosix.Api.Data;
using Tosix.Api.Entities;
using Tosix.Api.Security;
using Tosix.Api.Services;

namespace Tosix.Api.Controllers;

[ApiController]
[Route("api/admin/banners")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminBannersController(AppDbContext db, ILocalFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BannerDto>>> List(CancellationToken cancellationToken)
    {
        var items = await db.Banners.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => x.ToDto()).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<BannerDto>> Create([FromBody] BannerCreateDto body, CancellationToken cancellationToken)
    {
        var entity = new Banner
        {
            Id = Guid.NewGuid(),
            ImagePath = body.ImagePath,
            LinkUrl = body.LinkUrl,
            SortOrder = body.SortOrder,
            IsActive = body.IsActive
        };
        db.Banners.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BannerDto>> Update(Guid id, [FromBody] BannerUpdateDto body, CancellationToken cancellationToken)
    {
        var entity = await db.Banners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (body.ImagePath != entity.ImagePath) storage.DeleteIfExists(entity.ImagePath);
        entity.ImagePath = body.ImagePath;
        entity.LinkUrl = body.LinkUrl;
        entity.SortOrder = body.SortOrder;
        entity.IsActive = body.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Banners.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        storage.DeleteIfExists(entity.ImagePath);
        db.Banners.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/feedback")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminFeedbackController(AppDbContext db, ILocalFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeedbackImageDto>>> List(CancellationToken cancellationToken)
    {
        var items = await db.FeedbackImages.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => x.ToDto()).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<FeedbackImageDto>> Create([FromBody] FeedbackImageCreateDto body, CancellationToken cancellationToken)
    {
        var entity = new FeedbackImage
        {
            Id = Guid.NewGuid(),
            ImagePath = body.ImagePath,
            Caption = body.Caption,
            SortOrder = body.SortOrder,
            IsActive = body.IsActive
        };
        db.FeedbackImages.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FeedbackImageDto>> Update(Guid id, [FromBody] FeedbackImageUpdateDto body, CancellationToken cancellationToken)
    {
        var entity = await db.FeedbackImages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (body.ImagePath != entity.ImagePath) storage.DeleteIfExists(entity.ImagePath);
        entity.ImagePath = body.ImagePath;
        entity.Caption = body.Caption;
        entity.SortOrder = body.SortOrder;
        entity.IsActive = body.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.FeedbackImages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        storage.DeleteIfExists(entity.ImagePath);
        db.FeedbackImages.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminReviewsController(AppDbContext db, ILocalFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerReviewDto>>> List(CancellationToken cancellationToken)
    {
        var items = await db.CustomerReviews.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => x.ToDto()).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerReviewDto>> Create([FromBody] CustomerReviewCreateDto body, CancellationToken cancellationToken)
    {
        var entity = new CustomerReview
        {
            Id = Guid.NewGuid(),
            ImagePath = body.ImagePath,
            SortOrder = body.SortOrder,
            IsActive = body.IsActive
        };
        db.CustomerReviews.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerReviewDto>> Update(Guid id, [FromBody] CustomerReviewUpdateDto body, CancellationToken cancellationToken)
    {
        var entity = await db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (body.ImagePath != entity.ImagePath) storage.DeleteIfExists(entity.ImagePath);
        entity.ImagePath = body.ImagePath;
        entity.SortOrder = body.SortOrder;
        entity.IsActive = body.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        storage.DeleteIfExists(entity.ImagePath);
        db.CustomerReviews.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminSettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteSettingDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.FirstOrDefaultAsync(cancellationToken);
        return settings is null ? NotFound() : Ok(settings.ToDto());
    }

    [HttpPut]
    public async Task<ActionResult<SiteSettingDto>> Update([FromBody] SiteSettingUpdateDto body, CancellationToken cancellationToken)
    {
        var settings = await db.SiteSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new SiteSetting { Id = Guid.NewGuid() };
            db.SiteSettings.Add(settings);
        }

        settings.CompanyName = body.CompanyName.Trim();
        settings.TaxCode = body.TaxCode.Trim();
        settings.Address = body.Address.Trim();
        settings.Email = body.Email.Trim();
        settings.PhonePrimary = body.PhonePrimary.Trim();
        settings.PhoneSecondary = body.PhoneSecondary?.Trim();
        settings.FacebookUrl = body.FacebookUrl?.Trim();
        settings.ZaloUrl = body.ZaloUrl?.Trim();
        settings.ZaloQrImagePath = body.ZaloQrImagePath?.Trim();
        settings.SiteTitle = body.SiteTitle?.Trim();
        settings.SiteTagline = body.SiteTagline?.Trim();
        settings.HeroEyebrow = body.HeroEyebrow?.Trim();
        settings.LogoSubtitle = body.LogoSubtitle?.Trim();
        settings.Trust1Title = body.Trust1Title?.Trim();
        settings.Trust1Text = body.Trust1Text?.Trim();
        settings.Trust2Title = body.Trust2Title?.Trim();
        settings.Trust2Text = body.Trust2Text?.Trim();
        settings.Trust3Title = body.Trust3Title?.Trim();
        settings.Trust3Text = body.Trust3Text?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(settings.ToDto());
    }
}

[ApiController]
[Route("api/admin")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { ok = true, message = "Tosix admin API" });
}

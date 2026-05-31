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
[Route("api/admin/categories")]
[Authorize(Roles = TosixRoles.Admin)]
public sealed class AdminCategoriesController(AppDbContext db, ILocalFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> List(CancellationToken cancellationToken)
    {
        var items = await db.ProductCategories.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(c => c.ToDto(c.Products.Count))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryCreateDto body, CancellationToken cancellationToken)
    {
        var entity = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = body.Name.Trim(),
            Slug = body.Slug.Trim().ToLowerInvariant(),
            ImagePath = body.ImagePath,
            SortOrder = body.SortOrder,
            IsActive = body.IsActive
        };
        db.ProductCategories.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(entity.ToDto());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] CategoryUpdateDto body, CancellationToken cancellationToken)
    {
        var entity = await db.ProductCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        if (body.ImagePath != entity.ImagePath)
            storage.DeleteIfExists(entity.ImagePath);

        entity.Name = body.Name.Trim();
        entity.Slug = body.Slug.Trim().ToLowerInvariant();
        entity.ImagePath = body.ImagePath;
        entity.SortOrder = body.SortOrder;
        entity.IsActive = body.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        var count = await db.Products.CountAsync(p => p.CategoryId == id, cancellationToken);
        return Ok(entity.ToDto(count));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ProductCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (await db.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
            return BadRequest("Không thể xóa nhóm đang có sản phẩm.");

        storage.DeleteIfExists(entity.ImagePath);
        db.ProductCategories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

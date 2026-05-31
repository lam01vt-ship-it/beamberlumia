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

[Route("api/admin/products")]

[Authorize(Roles = TosixRoles.Admin)]

public sealed class AdminProductsController(AppDbContext db, ILocalFileStorage storage) : ControllerBase

{

    [HttpGet]

    public async Task<ActionResult<PagedResultDto<ProductDto>>> List(

        [FromQuery] string? q,

        [FromQuery] Guid? categoryId,

        [FromQuery] int page = 1,

        [FromQuery] int pageSize = 20,

        CancellationToken cancellationToken = default)

    {

        page = Math.Max(1, page);

        pageSize = Math.Clamp(pageSize, 1, 100);



        var query = db.Products.AsNoTracking()

            .Include(p => p.Category)

            .AsQueryable();



        if (categoryId.HasValue)

            query = query.Where(p => p.CategoryId == categoryId.Value);



        query = query.ApplySearch(q);



        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query

            .OrderBy(p => p.SortOrder)

            .Skip((page - 1) * pageSize)

            .Take(pageSize)

            .Select(p => new ProductDto(

                p.Id,

                p.CategoryId,

                p.Category.Name,

                p.Code,

                p.Name,

                p.Price,

                p.ImagePath,

                p.IsNew,

                p.IsFeatured,

                p.IsInStock,

                p.IsOrder,

                p.IsUpdating,

                p.SortOrder,

                p.IsActive,

                p.Images.Count))

            .ToListAsync(cancellationToken);



        return Ok(new PagedResultDto<ProductDto>(

            items,

            page,

            pageSize,

            totalCount,

            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)));

    }



    [HttpGet("{id:guid}")]

    public async Task<ActionResult<AdminProductDetailDto>> Get(Guid id, CancellationToken cancellationToken)

    {

        var entity = await db.Products.AsNoTracking()

            .Include(p => p.Category)

            .Include(p => p.Images)

            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);



        return entity is null ? NotFound() : Ok(entity.ToAdminDetailDto());

    }



    [HttpPost]

    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto body, CancellationToken cancellationToken)

    {

        if (!await db.ProductCategories.AnyAsync(c => c.Id == body.CategoryId, cancellationToken))

            return BadRequest("Nhóm sản phẩm không tồn tại.");

        var stockError = ValidateStockFlags(body.IsInStock, body.IsOrder, body.IsUpdating);
        if (stockError is not null) return BadRequest(stockError);



        var paths = ProductImageHelper.NormalizePaths(body.ImagePaths);

        var entity = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = body.CategoryId,
            Code = body.Code.Trim(),
            Name = body.Name.Trim(),
            Price = body.Price,
            IsNew = body.IsNew,
            IsFeatured = body.IsFeatured,
            IsInStock = body.IsInStock,
            IsOrder = body.IsOrder,
            IsUpdating = body.IsUpdating,
            SortOrder = body.SortOrder,
            IsActive = body.IsActive
        };

        ProductImageHelper.AttachGallery(entity, paths);

        db.Products.Add(entity);

        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(entity).Reference(p => p.Category).LoadAsync(cancellationToken);

        return Ok(entity.ToDto(paths.Count));

    }



    [HttpPut("{id:guid}")]

    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] ProductUpdateDto body, CancellationToken cancellationToken)

    {

        var entity = await db.Products

            .Include(p => p.Category)

            .Include(p => p.Images)

            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null) return NotFound();

        var stockError = ValidateStockFlags(body.IsInStock, body.IsOrder, body.IsUpdating);
        if (stockError is not null) return BadRequest(stockError);



        var paths = ProductImageHelper.NormalizePaths(body.ImagePaths);

        var oldPaths = ProductImageHelper.GetGalleryPaths(entity);

        ProductImageHelper.DeleteRemovedFiles(oldPaths, paths, storage);

        await ProductImageHelper.ReplaceGalleryAsync(db, entity, paths, cancellationToken);



        entity.CategoryId = body.CategoryId;

        entity.Code = body.Code.Trim();

        entity.Name = body.Name.Trim();

        entity.Price = body.Price;

        entity.IsNew = body.IsNew;

        entity.IsFeatured = body.IsFeatured;

        entity.IsInStock = body.IsInStock;

        entity.IsOrder = body.IsOrder;

        entity.IsUpdating = body.IsUpdating;

        entity.SortOrder = body.SortOrder;

        entity.IsActive = body.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(entity.ToDto(paths.Count));

    }



    [HttpDelete("{id:guid}")]

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)

    {

        var entity = await db.Products

            .Include(p => p.Images)

            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null) return NotFound();



        foreach (var path in ProductImageHelper.GetGalleryPaths(entity))

            storage.DeleteIfExists(path);



        db.Products.Remove(entity);

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();

    }

    private static string? ValidateStockFlags(bool isInStock, bool isOrder, bool isUpdating)
    {
        var selectedCount = (isInStock ? 1 : 0) + (isOrder ? 1 : 0) + (isUpdating ? 1 : 0);
        if (selectedCount == 0)
            return "Vui lòng chọn loại hàng.";
        if (selectedCount > 1)
            return "Sản phẩm chỉ được chọn một loại: Hàng có sẵn, Hàng order hoặc Đang cập nhật.";
        return null;
    }

}



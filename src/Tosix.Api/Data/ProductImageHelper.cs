using Microsoft.EntityFrameworkCore;
using Tosix.Api.Entities;
using Tosix.Api.Services;

namespace Tosix.Api.Data;

public static class ProductImageHelper
{
    public static List<string> NormalizePaths(IReadOnlyList<string>? imagePaths, string? fallbackImagePath = null)
    {
        var paths = (imagePaths ?? [])
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (paths.Count == 0 && !string.IsNullOrWhiteSpace(fallbackImagePath))
            paths.Add(fallbackImagePath.Trim());

        return paths;
    }

    public static List<string> GetGalleryPaths(Product product)
    {
        var gallery = product.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => i.ImagePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (gallery.Count == 0 && !string.IsNullOrWhiteSpace(product.ImagePath))
            gallery.Add(product.ImagePath);

        return gallery;
    }

    public static void AttachGallery(Product product, IReadOnlyList<string> paths)
    {
        product.Images.Clear();
        var order = 0;
        foreach (var path in paths)
        {
            product.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImagePath = path,
                SortOrder = order++,
            });
        }

        product.ImagePath = paths.FirstOrDefault();
    }

    public static async Task ReplaceGalleryAsync(
        AppDbContext db,
        Product product,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken = default)
    {
        foreach (var img in product.Images.ToList())
            db.Entry(img).State = EntityState.Detached;

        product.Images.Clear();

        await db.ProductImages
            .Where(i => i.ProductId == product.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var order = 0;
        foreach (var path in paths)
        {
            db.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ImagePath = path,
                SortOrder = order++,
            });
        }

        product.ImagePath = paths.FirstOrDefault();

        await db.SaveChangesAsync(cancellationToken);
    }

    public static void DeleteRemovedFiles(
        IEnumerable<string> oldPaths,
        IEnumerable<string> newPaths,
        ILocalFileStorage storage)
    {
        var newSet = new HashSet<string>(newPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in oldPaths)
        {
            if (!newSet.Contains(path))
                storage.DeleteIfExists(path);
        }
    }
}

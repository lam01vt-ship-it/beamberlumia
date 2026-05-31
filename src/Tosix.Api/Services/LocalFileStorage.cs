namespace Tosix.Api.Services;

public interface ILocalFileStorage
{
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    void DeleteIfExists(string? relativePath);
}

public sealed class LocalFileStorage(IWebHostEnvironment env) : ILocalFileStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("File rỗng.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException("Chỉ chấp nhận ảnh jpg, jpeg, png, webp, gif.");

        var safeFolder = folder.Trim('/').Replace('\\', '/');
        var uploadsRoot = Path.Combine(env.WebRootPath, "uploads", safeFolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/{safeFolder}/{fileName}";
    }

    public void DeleteIfExists(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.WebRootPath, normalized);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}

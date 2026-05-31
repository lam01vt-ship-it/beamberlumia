using System.Text.RegularExpressions;

namespace Tosix.Api.Services;

public sealed class SeedImageResolver
{
    private static readonly Regex SizeSuffix = new(@"-\d+x\d+(?=\.[^.]+$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string _seedDir;
    private readonly string _importDir;
    private readonly Dictionary<string, string> _bestByStem;

    public SeedImageResolver(IWebHostEnvironment env)
    {
        _seedDir = Path.Combine(env.WebRootPath, "uploads", "seed");
        _importDir = Path.Combine(env.WebRootPath, "uploads", "imported");
        Directory.CreateDirectory(_seedDir);
        Directory.CreateDirectory(_importDir);
        _bestByStem = BuildIndex();
    }

    public string? ResolveLocalSeedFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var exactPath = Path.Combine(_seedDir, fileName);
        if (File.Exists(exactPath))
            return $"/uploads/seed/{fileName}";

        if (_bestByStem.TryGetValue(GetStem(fileName), out var localName))
            return $"/uploads/seed/{localName}";

        return null;
    }

    public string? ResolveExisting(string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
            return null;

        var fileName = Path.GetFileName(new Uri(remoteUrl).LocalPath);
        if (_bestByStem.TryGetValue(GetStem(fileName), out var localName))
            return $"/uploads/seed/{localName}";

        return null;
    }

    public async Task<string?> ResolveOrDownloadAsync(string? remoteUrl, HttpClient http, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
            return null;

        var existing = ResolveExisting(remoteUrl);
        if (existing is not null)
            return existing;

        var fileName = Path.GetFileName(new Uri(remoteUrl).LocalPath);
        var stem = GetStem(fileName);
        var targetName = $"{stem}{Path.GetExtension(fileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(_importDir, targetName);

        if (!File.Exists(fullPath))
        {
            await using var stream = await http.GetStreamAsync(remoteUrl, cancellationToken);
            await using var file = File.Create(fullPath);
            await stream.CopyToAsync(file, cancellationToken);
        }

        return $"/uploads/imported/{targetName}";
    }

    private Dictionary<string, string> BuildIndex()
    {
        var candidates = new Dictionary<string, (string fileName, long size, bool hasSizeSuffix)>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(_seedDir))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(_seedDir))
        {
            if (!IsImage(path))
                continue;

            var fileName = Path.GetFileName(path);
            var stem = GetStem(fileName);
            var size = new FileInfo(path).Length;
            var hasSuffix = SizeSuffix.IsMatch(fileName);

            if (!candidates.TryGetValue(stem, out var current)
                || (!hasSuffix && current.hasSizeSuffix)
                || (hasSuffix == current.hasSizeSuffix && size > current.size)
                || (!hasSuffix && !current.hasSizeSuffix && size > current.size))
            {
                candidates[stem] = (fileName, size, hasSuffix);
            }
        }

        return candidates.ToDictionary(x => x.Key, x => x.Value.fileName, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetStem(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        return SizeSuffix.Replace(stem, string.Empty);
    }

    private static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }
}

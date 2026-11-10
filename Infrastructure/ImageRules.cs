namespace RaceDay.Mvc.Infrastructure;

/// <summary>Front-door checks so obviously bad uploads are rejected with a friendly message before they reach the API.</summary>
public static class ImageRules
{
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null; // optional upload

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return "Upload a JPG, PNG or WebP image.";

        if (file.Length > MaxBytes) return "That image is too large. The limit is 5 MB.";

        return null;
    }
}

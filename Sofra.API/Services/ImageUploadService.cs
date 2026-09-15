using Microsoft.AspNetCore.Http;
using Sofra.API.Exceptions;
using Sofra.API.Services.Interfaces;

namespace Sofra.API.Services;

public class ImageUploadService(IWebHostEnvironment env, ILogger<ImageUploadService> logger) : IImageUploadService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string UploadsUrlPrefix = "/images/uploads/";

    public async Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new BusinessException("Fajl je prazan.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new BusinessException("Slika ne smije biti veća od 5 MB.");
        }

        var extension = await DetectExtensionAsync(file, cancellationToken);
        if (extension is null)
        {
            throw new BusinessException("Dozvoljeni formati slike su JPG, PNG i WEBP (provjera po stvarnom sadržaju fajla).");
        }

        var uploadsRoot = Path.Combine(env.WebRootPath, "images", "uploads", subfolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var target = new FileStream(fullPath, FileMode.Create))
        await using (var source = file.OpenReadStream())
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        return $"{UploadsUrlPrefix}{subfolder}/{fileName}";
    }

    public void DeleteIfExists(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith(UploadsUrlPrefix, StringComparison.Ordinal))
        {
            // Seed slike (/images/seed/...) ili prazna vrijednost - nema sta brisati.
            return;
        }

        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(env.WebRootPath, relativePath);

        if (!File.Exists(fullPath))
        {
            return;
        }

        try
        {
            File.Delete(fullPath);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Ne mogu obrisati staru sliku {Path}.", fullPath);
        }
    }

    private static async Task<string?> DetectExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, 12), cancellationToken);

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ".png";
        }

        // WEBP = RIFF....WEBP: bajtovi 0-3 "RIFF", bajtovi 8-11 "WEBP".
        if (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ".webp";
        }

        return null;
    }
}

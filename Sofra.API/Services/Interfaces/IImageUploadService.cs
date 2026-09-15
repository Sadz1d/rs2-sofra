using Microsoft.AspNetCore.Http;

namespace Sofra.API.Services.Interfaces;

public interface IImageUploadService
{
    /// <summary>
    /// Validira sadrzaj fajla (magic bytes, ne ekstenziju), snima ga pod novim GUID imenom u
    /// wwwroot/images/uploads/{subfolder}/ i vraca relativnu putanju (npr. "/images/uploads/menu-items/&lt;guid&gt;.jpg").
    /// </summary>
    Task<string> SaveAsync(IFormFile file, string subfolder, CancellationToken cancellationToken = default);

    /// <summary>Brise fajl na koji pokazuje relativna putanja, ako postoji. Tiho ignorise seed/vanjske putanje.</summary>
    void DeleteIfExists(string? relativeUrl);
}

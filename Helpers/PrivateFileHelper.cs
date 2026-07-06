using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using KayAyiti.DTOs;

namespace KayAyiti.Helpers;

// Depo prive pou dokiman sansib (CIN/paspò, selfie, tit pwopriyete).
// Toujou anrejistre anba ContentRootPath (pa WebRootPath) pou UseStaticFiles() pa janm ka sèvi yo.
public static class PrivateFileHelper
{
    private static readonly FileExtensionContentTypeProvider _ctProvider = new();

    public static async Task<string> SaveAsync(IWebHostEnvironment env, string subfolder, IFormFile file)
    {
        var dir = Path.Combine(env.ContentRootPath, "private_uploads", subfolder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";

        await using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
        await file.CopyToAsync(stream);

        return fileName;
    }

    public static void DeleteIfExists(IWebHostEnvironment env, string subfolder, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;
        var path = Path.Combine(env.ContentRootPath, "private_uploads", subfolder, fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    public static IActionResult Serve(IWebHostEnvironment env, string subfolder, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return new NotFoundObjectResult(ApiResponse<bool>.Fail("Dokiman pa disponib."));

        var path = Path.Combine(env.ContentRootPath, "private_uploads", subfolder, fileName);
        if (!File.Exists(path))
            return new NotFoundObjectResult(ApiResponse<bool>.Fail("Dokiman pa jwenn sou disk."));

        if (!_ctProvider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        return new PhysicalFileResult(path, contentType);
    }
}

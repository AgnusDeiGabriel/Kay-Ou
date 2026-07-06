using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Helpers;

namespace KayAyiti.Services;

public interface IKycService
{
    Task<ApiResponse<KycStatusDto>> SubmitAsync(int userId, IFormFile idDocument, IFormFile selfie);
    Task<ApiResponse<KycStatusDto>> GetStatusAsync(int userId);
    Task<ApiResponse<PagedResult<AdminKycUserDto>>> GetQueueAsync(int page, int pageSize, string? status);
    Task<ApiResponse<UserDto>> AdminActionAsync(int userId, AdminKycActionRequest req, int adminId);
}

public class KycService : IKycService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private const long MaxSize = 5 * 1024 * 1024;

    public KycService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ApiResponse<KycStatusDto>> SubmitAsync(int userId, IFormFile idDocument, IFormFile selfie)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ApiResponse<KycStatusDto>.Fail("Itilizatè pa jwenn.");

        if (user.KycStatus == "Verified")
            return ApiResponse<KycStatusDto>.Fail("Idantite ou deja verifye.");

        if (idDocument is null || idDocument.Length == 0)
            return ApiResponse<KycStatusDto>.Fail("Ou dwe voye yon foto kat idantite oswa paspò.");
        if (selfie is null || selfie.Length == 0)
            return ApiResponse<KycStatusDto>.Fail("Ou dwe voye yon selfie.");

        foreach (var file in new[] { idDocument, selfie })
        {
            if (!AllowedTypes.Contains(file.ContentType.ToLower()))
                return ApiResponse<KycStatusDto>.Fail($"Sèlman JPEG, PNG ak WebP aksepte. '{file.ContentType}' pa valid.");
            if (file.Length > MaxSize)
                return ApiResponse<KycStatusDto>.Fail("Chak foto pa dwe depase 5MB.");
        }

        // Efase ansyen dokiman yo si n ap soumèt ankò
        PrivateFileHelper.DeleteIfExists(_env, "kyc-id", user.IdDocumentPath);
        PrivateFileHelper.DeleteIfExists(_env, "kyc-selfie", user.SelfiePath);

        user.IdDocumentPath = await PrivateFileHelper.SaveAsync(_env, "kyc-id", idDocument);
        user.SelfiePath = await PrivateFileHelper.SaveAsync(_env, "kyc-selfie", selfie);
        user.KycStatus = "Pending";
        user.KycSubmittedAt = DateTime.UtcNow;
        user.KycVerifiedAt = null;
        user.KycRejectionReason = null;

        await _db.SaveChangesAsync();

        return ApiResponse<KycStatusDto>.Ok(MapStatus(user), "Dokiman ou yo voye! Admin ap revize yo.");
    }

    public async Task<ApiResponse<KycStatusDto>> GetStatusAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ApiResponse<KycStatusDto>.Fail("Itilizatè pa jwenn.");
        return ApiResponse<KycStatusDto>.Ok(MapStatus(user));
    }

    public async Task<ApiResponse<PagedResult<AdminKycUserDto>>> GetQueueAsync(int page, int pageSize, string? status)
    {
        var query = _db.Users.Where(u => u.IdDocumentPath != null);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(u => u.KycStatus == status);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.KycSubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminKycUserDto
            {
                UserId = u.Id,
                Nom = u.Nom,
                Email = u.Email,
                Telefon = u.Telefon,
                KycStatus = u.KycStatus,
                KycRejectionReason = u.KycRejectionReason,
                KycSubmittedAt = u.KycSubmittedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResult<AdminKycUserDto>>.Ok(new PagedResult<AdminKycUserDto>
        {
            Items = users,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<UserDto>> AdminActionAsync(int userId, AdminKycActionRequest req, int adminId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return ApiResponse<UserDto>.Fail("Itilizatè pa jwenn.");
        if (user.KycStatus != "Pending")
            return ApiResponse<UserDto>.Fail("Verifikasyon sa a deja trete.");

        user.KycStatus = req.Action;
        user.KycRejectionReason = req.Action == "Rejected" ? req.Reason : null;
        user.KycVerifiedAt = req.Action == "Verified" ? DateTime.UtcNow : null;
        user.KycVerifiedByAdminId = adminId;

        await _db.SaveChangesAsync();
        return ApiResponse<UserDto>.Ok(
            AuthService.MapUser(user),
            $"Idantite {(req.Action == "Verified" ? "verifye" : "rejte")} avèk siksè.");
    }

    private static KycStatusDto MapStatus(Models.User u) => new()
    {
        KycStatus = u.KycStatus,
        KycRejectionReason = u.KycRejectionReason,
        KycSubmittedAt = u.KycSubmittedAt,
        KycVerifiedAt = u.KycVerifiedAt
    };
}

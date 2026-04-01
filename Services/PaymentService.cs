using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Models;

namespace KayAyiti.Services;

public interface IPaymentService
{
    Task<ApiResponse<PaymentDto>> InitiateAsync(int houseId, IFormFile resi, int userId);
    Task<ApiResponse<PagedResult<PaymentDto>>> GetAllAsync(int page, int pageSize, string? statut);
    Task<ApiResponse<List<PaymentDto>>> GetMyPaymentsAsync(int userId);
    Task<ApiResponse<PaymentDto>> AdminActionAsync(int id, AdminPaymentActionRequest req, int adminId);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public PaymentService(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
    }

    public async Task<ApiResponse<PaymentDto>> InitiateAsync(
        int houseId, IFormFile resi, int userId)
    {
        var house = await _db.Houses.FindAsync(houseId);
        if (house is null) return ApiResponse<PaymentDto>.Fail("Kay pa jwenn.");
        if (house.Statut != "Approved")
            return ApiResponse<PaymentDto>.Fail("Kay sa a pako apwouve pa admin.");

        // Verifye si itilizatè a deja peye
        var existingPayment = await _db.Payments.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.HouseId == houseId && p.Statut != "Rejected");

        if (existingPayment != null)
            return ApiResponse<PaymentDto>.Fail(
                existingPayment.Statut == "Verified"
                    ? "Ou deja peye pou kay sa a."
                    : "Peman ou an poko verifye. Tann admin.");

        // Validate resi
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
        if (!allowedTypes.Contains(resi.ContentType.ToLower()))
            return ApiResponse<PaymentDto>.Fail("Resi dwe JPEG oswa PNG.");
        if (resi.Length > 5 * 1024 * 1024)
            return ApiResponse<PaymentDto>.Fail("Resi pa dwe depase 5MB.");

        // Sove resi
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "payments");
        Directory.CreateDirectory(uploadDir);
        var fileName = $"resi_{userId}_{houseId}_{Guid.NewGuid()}{Path.GetExtension(resi.FileName)}";
        var filePath = Path.Combine(uploadDir, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await resi.CopyToAsync(stream);

        var amount = decimal.Parse(_config["Payment:MontanHTG"] ?? "1500");

        var payment = new Payment
        {
            UserId = userId,
            HouseId = houseId,
            Amount = amount,
            ResiImageUrl = $"/uploads/payments/{fileName}",
            Statut = "Pending"
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return ApiResponse<PaymentDto>.Ok(
            await MapPaymentAsync(payment.Id),
            "Resi voye! Admin ap verifye nan 24h.");
    }

    public async Task<ApiResponse<PagedResult<PaymentDto>>> GetAllAsync(
        int page, int pageSize, string? statut)
    {
        var query = _db.Payments.AsQueryable();

        if (!string.IsNullOrEmpty(statut))
            query = query.Where(p => p.Statut == statut);

        var total = await query.CountAsync();
        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.Id)
            .ToListAsync();

        var items = new List<PaymentDto>();
        foreach (var id in payments)
            items.Add(await MapPaymentAsync(id));

        return ApiResponse<PagedResult<PaymentDto>>.Ok(new PagedResult<PaymentDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<List<PaymentDto>>> GetMyPaymentsAsync(int userId)
    {
        var ids = await _db.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.Id)
            .ToListAsync();

        var items = new List<PaymentDto>();
        foreach (var id in ids)
            items.Add(await MapPaymentAsync(id));

        return ApiResponse<List<PaymentDto>>.Ok(items);
    }

    public async Task<ApiResponse<PaymentDto>> AdminActionAsync(
        int id, AdminPaymentActionRequest req, int adminId)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment is null) return ApiResponse<PaymentDto>.Fail("Peman pa jwenn.");
        if (payment.Statut != "Pending")
            return ApiResponse<PaymentDto>.Fail("Peman sa a deja trete.");

        payment.Statut = req.Action;
        payment.NoteAdmin = req.NoteAdmin;
        payment.VerifiedByAdminId = adminId;
        payment.VerifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<PaymentDto>.Ok(
            await MapPaymentAsync(id),
            $"Peman {(req.Action == "Verified" ? "verifye" : "rejte")} avèk siksè.");
    }

    // ─── Mapper ──────────────────────────────────────────────────────────────

    private async Task<PaymentDto> MapPaymentAsync(int id)
    {
        var p = await _db.Payments
            .Include(x => x.User)
            .Include(x => x.House)
                .ThenInclude(h => h.User)
            .Include(x => x.House)
                .ThenInclude(h => h.Images)
            .FirstAsync(x => x.Id == id);

        return new PaymentDto
        {
            Id = p.Id,
            Amount = p.Amount,
            Statut = p.Statut,
            ResiImageUrl = p.ResiImageUrl,
            NoteAdmin = p.NoteAdmin,
            CreatedAt = p.CreatedAt,
            VerifiedAt = p.VerifiedAt,
            User = AuthService.MapUser(p.User),
            House = new HouseDto
            {
                Id = p.House.Id,
                Titre = p.House.Titre,
                Zone = p.House.Zone,
                Prix = p.House.Prix,
                Type = p.House.Type,
                Statut = p.House.Statut,
                Images = p.House.Images.Select(i => i.ImageUrl).ToList(),
                Owner = AuthService.MapUser(p.House.User),
                Description = p.House.Description,
                Chambres = p.House.Chambres,
                Beny = p.House.Beny,
                Superficie = p.House.Superficie
            }
        };
    }
}
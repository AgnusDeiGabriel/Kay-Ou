using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Helpers;
using KayAyiti.Models;

namespace KayAyiti.Services;

public interface IHouseService
{
    Task<ApiResponse<HouseDto>> CreateAsync(CreateHouseRequest req, int userId);
    Task<ApiResponse<PagedResult<HouseDto>>> GetAllAsync(HouseFilter filter, int? currentUserId, bool isAdmin = false);
    Task<ApiResponse<HouseDto>> GetByIdAsync(int id, int? currentUserId, bool isAdmin = false);
    Task<ApiResponse<HouseDto>> UpdateAsync(int id, UpdateHouseRequest req, int userId);
    Task<ApiResponse<bool>> DeleteAsync(int id, int userId, string userRole);
    Task<ApiResponse<HouseDto>> AdminActionAsync(int id, AdminHouseActionRequest req, int adminId);
    Task<ApiResponse<List<string>>> UploadImagesAsync(int houseId, IFormFileCollection files, int userId);
    Task<ApiResponse<List<HouseDto>>> GetMyHousesAsync(int userId);
    Task<ApiResponse<object>> ToggleLikeAsync(int houseId, int userId);

    // ─── Verifikasyon pwopriyete ───────────────────────────────────────────
    Task<ApiResponse<HouseDto>> SubmitOwnershipDocsAsync(
        int houseId, int userId, IFormFile titleDeed, IFormFile? mandate, IFormFile? taxReceipt,
        string? adresseComplete, decimal? latitude, decimal? longitude);
    Task<ApiResponse<PagedResult<HouseDto>>> GetOwnershipQueueAsync(int page, int pageSize, string? status);
    Task<ApiResponse<HouseDto>> OwnershipAdminActionAsync(int houseId, AdminOwnershipActionRequest req, int adminId);
}

public class HouseService : IHouseService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public HouseService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ApiResponse<HouseDto>> CreateAsync(CreateHouseRequest req, int userId)
    {
        var house = new House
        {
            Titre = req.Titre.Trim(),
            Description = req.Description.Trim(),
            Prix = req.Prix,
            Zone = req.Zone.Trim(),
            Type = req.Type,
            Chambres = req.Chambres,
            Beny = req.Beny,
            Superficie = req.Superficie,
            Garaj = req.Garaj,
            Sekirite = req.Sekirite,
            Piscine = req.Piscine,
            UserId = userId,
            Statut = "Pending"
        };

        _db.Houses.Add(house);
        await _db.SaveChangesAsync();

        await _db.Entry(house).Reference(h => h.User).LoadAsync();
        await _db.Entry(house).Collection(h => h.Images).LoadAsync();

        return ApiResponse<HouseDto>.Ok(
            await MapHouseAsync(house, userId),
            "Kay ou a voye pou validasyon admin!");
    }

    public async Task<ApiResponse<PagedResult<HouseDto>>> GetAllAsync(
        HouseFilter filter, int? currentUserId, bool isAdmin = false)
    {
        var query = _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .AsQueryable();

        // Si se yon itilizatè nòmal: montre sèlman approved
        if (filter.Statut == null)
            query = query.Where(h => h.Statut == "Approved");
        else
            query = query.Where(h => h.Statut == filter.Statut);

        if (!string.IsNullOrEmpty(filter.Zone))
            query = query.Where(h => h.Zone.Contains(filter.Zone));

        if (!string.IsNullOrEmpty(filter.Type))
            query = query.Where(h => h.Type == filter.Type);

        if (filter.PrixMin.HasValue)
            query = query.Where(h => h.Prix >= filter.PrixMin);

        if (filter.PrixMax.HasValue)
            query = query.Where(h => h.Prix <= filter.PrixMax);

        if (filter.ChambreMin.HasValue)
            query = query.Where(h => h.Chambres >= filter.ChambreMin);

        if (filter.ChambreMax.HasValue)
            query = query.Where(h => h.Chambres <= filter.ChambreMax);

        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(h =>
                h.Titre.Contains(filter.Search) ||
                h.Description.Contains(filter.Search) ||
                h.Zone.Contains(filter.Search));

        var total = await query.CountAsync();

        var houses = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var houseIds = houses.Select(h => h.Id).ToList();
        var likeCounts = await _db.HouseLikes
            .Where(l => houseIds.Contains(l.HouseId))
            .GroupBy(l => l.HouseId)
            .Select(g => new { HouseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HouseId, x => x.Count);

        HashSet<int> userLikedSet = currentUserId.HasValue
            ? (await _db.HouseLikes
                .Where(l => houseIds.Contains(l.HouseId) && l.UserId == currentUserId.Value)
                .Select(l => l.HouseId)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        var items = new List<HouseDto>();
        foreach (var h in houses)
        {
            likeCounts.TryGetValue(h.Id, out var lc);
            items.Add(await MapHouseAsync(h, currentUserId, isAdmin, lc, userLikedSet.Contains(h.Id)));
        }

        return ApiResponse<PagedResult<HouseDto>>.Ok(new PagedResult<HouseDto>
        {
            Items = items,
            Total = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<ApiResponse<HouseDto>> GetByIdAsync(int id, int? currentUserId, bool isAdmin = false)
    {
        var house = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (house is null)
            return ApiResponse<HouseDto>.Fail("Kay pa jwenn.");

        var likeCount = await _db.HouseLikes.CountAsync(l => l.HouseId == id);
        var isLiked   = currentUserId.HasValue &&
                        await _db.HouseLikes.AnyAsync(l => l.HouseId == id && l.UserId == currentUserId.Value);

        return ApiResponse<HouseDto>.Ok(await MapHouseAsync(house, currentUserId, isAdmin, likeCount, isLiked));
    }

    public async Task<ApiResponse<HouseDto>> UpdateAsync(
        int id, UpdateHouseRequest req, int userId)
    {
        var house = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (house is null) return ApiResponse<HouseDto>.Fail("Kay pa jwenn.");
        if (house.UserId != userId)
            return ApiResponse<HouseDto>.Fail("Ou pa ka modifye kay sa a.");

        if (req.Titre != null) house.Titre = req.Titre.Trim();
        if (req.Description != null) house.Description = req.Description.Trim();
        if (req.Prix.HasValue) house.Prix = req.Prix.Value;
        if (req.Zone != null) house.Zone = req.Zone.Trim();
        if (req.Type != null) house.Type = req.Type;
        if (req.Chambres.HasValue) house.Chambres = req.Chambres.Value;
        if (req.Beny.HasValue) house.Beny = req.Beny.Value;
        if (req.Superficie.HasValue) house.Superficie = req.Superficie.Value;
        if (req.Garaj.HasValue) house.Garaj = req.Garaj.Value;
        if (req.Sekirite.HasValue) house.Sekirite = req.Sekirite.Value;
        if (req.Piscine.HasValue) house.Piscine = req.Piscine.Value;

        // Remete an Pending si modifye
        house.Statut = "Pending";

        await _db.SaveChangesAsync();
        return ApiResponse<HouseDto>.Ok(
            await MapHouseAsync(house, userId),
            "Kay modifye. Tann validasyon admin ankò.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, int userId, string userRole)
    {
        var house = await _db.Houses
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (house is null) return ApiResponse<bool>.Fail("Kay pa jwenn.");

        if (house.UserId != userId && userRole != "Admin")
            return ApiResponse<bool>.Fail("Ou pa ka efase kay sa a.");

        // Efase imaj sou disk
        foreach (var img in house.Images)
        {
            var filePath = Path.Combine(_env.WebRootPath,
                img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        // Efase dokiman pwopriyete prive sou disk
        PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipTitleDeedPath);
        PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipMandatePath);
        PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipTaxReceiptPath);

        _db.Houses.Remove(house);
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Kay efase avèk siksè.");
    }

    public async Task<ApiResponse<HouseDto>> AdminActionAsync(
        int id, AdminHouseActionRequest req, int adminId)
    {
        var house = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (house is null) return ApiResponse<HouseDto>.Fail("Kay pa jwenn.");

        house.Statut = req.Action;
        house.RejectionReason = req.Action == "Rejected" ? req.Reason : null;
        house.ApprovedAt = req.Action == "Approved" ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync();
        return ApiResponse<HouseDto>.Ok(
            await MapHouseAsync(house, adminId, isAdmin: true),
            $"Kay {(req.Action == "Approved" ? "apwouve" : "rejte")} avèk siksè.");
    }

    public async Task<ApiResponse<List<string>>> UploadImagesAsync(
        int houseId, IFormFileCollection files, int userId)
    {
        var house = await _db.Houses
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == houseId);

        if (house is null) return ApiResponse<List<string>>.Fail("Kay pa jwenn.");
        if (house.UserId != userId)
            return ApiResponse<List<string>>.Fail("Ou pa ka ajoute imaj pou kay sa a.");

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        var urls = new List<string>();
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "houses");
        Directory.CreateDirectory(uploadDir);

        foreach (var file in files)
        {
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return ApiResponse<List<string>>.Fail(
                    $"Sèlman JPEG, PNG ak WebP aksepte. '{file.ContentType}' pa valid.");

            if (file.Length > 10 * 1024 * 1024)
                return ApiResponse<List<string>>.Fail("Imaj pa dwe depase 10MB.");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var url = $"/uploads/houses/{fileName}";
            urls.Add(url);

            var isPrincipal = !house.Images.Any();
            house.Images.Add(new HouseImage
            {
                ImageUrl = url,
                IsPrincipal = isPrincipal,
                HouseId = houseId
            });
        }

        await _db.SaveChangesAsync();
        return ApiResponse<List<string>>.Ok(urls, $"{urls.Count} imaj uploade avèk siksè.");
    }

    public async Task<ApiResponse<List<HouseDto>>> GetMyHousesAsync(int userId)
    {
        var houses = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        var houseIds = houses.Select(h => h.Id).ToList();
        var likeCounts = await _db.HouseLikes
            .Where(l => houseIds.Contains(l.HouseId))
            .GroupBy(l => l.HouseId)
            .Select(g => new { HouseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.HouseId, x => x.Count);

        var items = new List<HouseDto>();
        foreach (var h in houses)
        {
            likeCounts.TryGetValue(h.Id, out var lc);
            items.Add(await MapHouseAsync(h, userId, isAdmin: false, lc, false));
        }

        return ApiResponse<List<HouseDto>>.Ok(items);
    }

    // ─── Mapper ──────────────────────────────────────────────────────────────

    private async Task<HouseDto> MapHouseAsync(House h, int? currentUserId, bool isAdmin = false,
        int likeCount = 0, bool isLiked = false)
    {
        bool isOwner = currentUserId.HasValue && currentUserId == h.UserId;

        bool hasPaid = false;
        if (currentUserId.HasValue && !isOwner)
            hasPaid = await _db.Payments.AnyAsync(p =>
                p.UserId == currentUserId && p.HouseId == h.Id && p.Statut == "Verified");

        bool canSeeContact = isAdmin || isOwner || hasPaid;

        // Adrès konplè + GPS se done sansib pou revizyon admin — pa gen dwa
        // parèt pou nenpòt moun anlè pwopriyetè a ak admin (pa itilize pou kat piblik la).
        bool canSeeOwnershipDetails = isAdmin || isOwner;

        var owner = AuthService.MapUser(h.User);
        if (!canSeeContact)
        {
            owner.Telefon = string.Empty;
            owner.Email = string.Empty;
        }

        return new HouseDto
        {
            Id = h.Id,
            Titre = h.Titre,
            Description = h.Description,
            Prix = h.Prix,
            Zone = h.Zone,
            Type = h.Type,
            Chambres = h.Chambres,
            Beny = h.Beny,
            Superficie = h.Superficie,
            Garaj = h.Garaj,
            Sekirite = h.Sekirite,
            Piscine = h.Piscine,
            Statut = h.Statut,
            RejectionReason = h.RejectionReason,
            CreatedAt = h.CreatedAt,
            Owner = owner,
            Images = h.Images.Select(i => i.ImageUrl).ToList(),
            UserHasPaid = hasPaid || isOwner,
            LikeCount = likeCount,
            IsLikedByCurrentUser = isLiked,
            OwnershipStatus = h.OwnershipStatus,
            OwnershipRejectionReason = h.OwnershipRejectionReason,
            HasTitleDeed = h.OwnershipTitleDeedPath != null,
            HasMandate = h.OwnershipMandatePath != null,
            HasTaxReceipt = h.OwnershipTaxReceiptPath != null,
            AdresseComplete = canSeeOwnershipDetails ? h.AdresseComplete : null,
            Latitude = canSeeOwnershipDetails ? h.Latitude : null,
            Longitude = canSeeOwnershipDetails ? h.Longitude : null
        };
    }

    public async Task<ApiResponse<object>> ToggleLikeAsync(int houseId, int userId)
    {
        var house = await _db.Houses.FindAsync(houseId);
        if (house is null) return ApiResponse<object>.Fail("Kay pa jwenn.");

        var existing = await _db.HouseLikes
            .FirstOrDefaultAsync(l => l.HouseId == houseId && l.UserId == userId);

        bool isLiked;
        if (existing != null)
        {
            _db.HouseLikes.Remove(existing);
            isLiked = false;
        }
        else
        {
            _db.HouseLikes.Add(new HouseLike { HouseId = houseId, UserId = userId });
            isLiked = true;
        }

        await _db.SaveChangesAsync();
        var likeCount = await _db.HouseLikes.CountAsync(l => l.HouseId == houseId);
        return ApiResponse<object>.Ok(new { isLiked, likeCount });
    }

    // ─── Verifikasyon pwopriyete ─────────────────────────────────────────────

    private static readonly string[] OwnershipDocTypes =
        { "image/jpeg", "image/jpg", "image/png", "image/webp", "application/pdf" };
    private const long OwnershipMaxSize = 10 * 1024 * 1024;

    public async Task<ApiResponse<HouseDto>> SubmitOwnershipDocsAsync(
        int houseId, int userId, IFormFile titleDeed, IFormFile? mandate, IFormFile? taxReceipt,
        string? adresseComplete, decimal? latitude, decimal? longitude)
    {
        var house = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == houseId);

        if (house is null) return ApiResponse<HouseDto>.Fail("Kay pa jwenn.");
        if (house.UserId != userId)
            return ApiResponse<HouseDto>.Fail("Ou pa ka soumèt dokiman pou kay sa a.");
        if (house.OwnershipStatus == "Verified")
            return ApiResponse<HouseDto>.Fail("Pwopriyete kay sa a deja verifye.");

        if (titleDeed is null || titleDeed.Length == 0)
            return ApiResponse<HouseDto>.Fail("Ou dwe voye tit pwopriyete a oswa kontra vant lan.");

        foreach (var file in new[] { titleDeed, mandate, taxReceipt })
        {
            if (file is null) continue;
            if (!OwnershipDocTypes.Contains(file.ContentType.ToLower()))
                return ApiResponse<HouseDto>.Fail($"Sèlman JPEG, PNG, WebP oswa PDF aksepte. '{file.ContentType}' pa valid.");
            if (file.Length > OwnershipMaxSize)
                return ApiResponse<HouseDto>.Fail("Chak dokiman pa dwe depase 10MB.");
        }

        PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipTitleDeedPath);
        house.OwnershipTitleDeedPath = await PrivateFileHelper.SaveAsync(_env, "house-ownership", titleDeed);

        if (mandate != null)
        {
            PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipMandatePath);
            house.OwnershipMandatePath = await PrivateFileHelper.SaveAsync(_env, "house-ownership", mandate);
        }
        if (taxReceipt != null)
        {
            PrivateFileHelper.DeleteIfExists(_env, "house-ownership", house.OwnershipTaxReceiptPath);
            house.OwnershipTaxReceiptPath = await PrivateFileHelper.SaveAsync(_env, "house-ownership", taxReceipt);
        }

        house.AdresseComplete = adresseComplete?.Trim();
        house.Latitude = latitude;
        house.Longitude = longitude;
        house.OwnershipStatus = "Pending";
        house.OwnershipSubmittedAt = DateTime.UtcNow;
        house.OwnershipRejectionReason = null;

        await _db.SaveChangesAsync();
        return ApiResponse<HouseDto>.Ok(
            await MapHouseAsync(house, userId),
            "Dokiman pwopriyete yo voye! Admin ap revize yo.");
    }

    public async Task<ApiResponse<PagedResult<HouseDto>>> GetOwnershipQueueAsync(int page, int pageSize, string? status)
    {
        var query = _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .Where(h => h.OwnershipTitleDeedPath != null);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(h => h.OwnershipStatus == status);

        var total = await query.CountAsync();
        var houses = await query
            .OrderByDescending(h => h.OwnershipSubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<HouseDto>();
        foreach (var h in houses)
            items.Add(await MapHouseAsync(h, h.UserId, isAdmin: true));

        return ApiResponse<PagedResult<HouseDto>>.Ok(new PagedResult<HouseDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<HouseDto>> OwnershipAdminActionAsync(int houseId, AdminOwnershipActionRequest req, int adminId)
    {
        var house = await _db.Houses
            .Include(h => h.User)
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == houseId);

        if (house is null) return ApiResponse<HouseDto>.Fail("Kay pa jwenn.");
        if (house.OwnershipStatus != "Pending")
            return ApiResponse<HouseDto>.Fail("Verifikasyon sa a deja trete.");

        house.OwnershipStatus = req.Action;
        house.OwnershipRejectionReason = req.Action == "Rejected" ? req.Reason : null;
        house.OwnershipVerifiedAt = req.Action == "Verified" ? DateTime.UtcNow : null;
        house.OwnershipVerifiedByAdminId = adminId;

        await _db.SaveChangesAsync();
        return ApiResponse<HouseDto>.Ok(
            await MapHouseAsync(house, adminId, isAdmin: true),
            $"Pwopriyete {(req.Action == "Verified" ? "verifye" : "rejte")} avèk siksè.");
    }
}
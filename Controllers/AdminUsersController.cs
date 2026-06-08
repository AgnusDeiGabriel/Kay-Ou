using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Services;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminUsersController(AppDbContext db) => _db = db;

    /// <summary>Wè tout itilizatè yo</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Nom.Contains(search) ||
                u.Email.Contains(search) ||
                u.Telefon.Contains(search));

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id           = u.Id,
                Nom          = u.Nom,
                Email        = u.Email,
                Telefon      = u.Telefon,
                Role         = u.Role,
                IsActive     = u.IsActive,
                CreatedAt    = u.CreatedAt,
                HouseCount   = u.Houses.Count,
                PaymentCount = u.Payments.Count
            })
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(new PagedResult<UserDto>
        {
            Items = users,
            Total = total,
            Page = page,
            PageSize = pageSize
        }));
    }

    /// <summary>Aktive / dezaktive yon kont itilizatè</summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return NotFound(ApiResponse<bool>.Fail("Itilizatè pa jwenn."));

        if (user.Role == "Admin")
            return BadRequest(ApiResponse<bool>.Fail("Ou pa ka dezaktive yon Admin."));

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Ok(
            AuthService.MapUser(user),
            user.IsActive ? "Kont aktive." : "Kont dezaktive."));
    }

    /// <summary>Efase yon itilizatè ak tout done li yo</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
            return NotFound(ApiResponse<bool>.Fail("Itilizatè pa jwenn."));

        if (user.Role == "Admin")
            return BadRequest(ApiResponse<bool>.Fail("Ou pa ka efase yon Admin."));

        // Efase peman itilizatè a te fè kòm achetè
        await _db.Payments
            .Where(p => p.UserId == id)
            .ExecuteDeleteAsync();

        // Efase peman pou kay itilizatè a posede
        var houseIds = await _db.Houses
            .Where(h => h.UserId == id)
            .Select(h => h.Id)
            .ToListAsync();

        if (houseIds.Count > 0)
            await _db.Payments
                .Where(p => houseIds.Contains(p.HouseId))
                .ExecuteDeleteAsync();

        // Efase itilizatè (cascade: kay, imaj kay, refresh tokens)
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Itilizatè efase avèk siksè."));
    }

    /// <summary>Statistik global pou dashboard admin</summary>
    [HttpGet("/api/admin/stats")]
    public async Task<IActionResult> Stats()
    {
        var stats = new
        {
            TotalUsers = await _db.Users.CountAsync(u => u.Role == "User"),
            TotalHouses = await _db.Houses.CountAsync(),
            HousesPending = await _db.Houses.CountAsync(h => h.Statut == "Pending"),
            HousesApproved = await _db.Houses.CountAsync(h => h.Statut == "Approved"),
            HousesRejected = await _db.Houses.CountAsync(h => h.Statut == "Rejected"),
            PaymentsPending = await _db.Payments.CountAsync(p => p.Statut == "Pending"),
            PaymentsVerified = await _db.Payments.CountAsync(p => p.Statut == "Verified"),
            TotalRevenueHTG = await _db.Payments
                .Where(p => p.Statut == "Verified")
                .SumAsync(p => p.Amount),
            RecentUsers = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => AuthService.MapUser(u))
                .ToListAsync()
        };

        return Ok(ApiResponse<object>.Ok(stats));
    }
}
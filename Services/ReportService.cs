using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Models;

namespace KayAyiti.Services;

public interface IReportService
{
    Task<ApiResponse<ReportDto>> CreateAsync(CreateReportRequest req, int reporterId);
    Task<ApiResponse<PagedResult<ReportDto>>> GetAllAsync(int page, int pageSize, string? status);
    Task<ApiResponse<ReportDto>> AdminActionAsync(int id, AdminReportActionRequest req, int adminId);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<ApiResponse<ReportDto>> CreateAsync(CreateReportRequest req, int reporterId)
    {
        var house = await _db.Houses.FindAsync(req.HouseId);
        if (house is null) return ApiResponse<ReportDto>.Fail("Kay pa jwenn.");

        var existing = await _db.Reports.FirstOrDefaultAsync(r =>
            r.HouseId == req.HouseId && r.ReporterId == reporterId && r.Status == "Pending");
        if (existing != null)
            return ApiResponse<ReportDto>.Fail("Ou deja rapòte kay sa a. Admin ap egzamine l.");

        var report = new Report
        {
            HouseId = req.HouseId,
            ReporterId = reporterId,
            Reason = req.Reason,
            Details = req.Details?.Trim()
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        return ApiResponse<ReportDto>.Ok(
            await MapReportAsync(report.Id),
            "Mèsi! Nou voye rapò a bay admin.");
    }

    public async Task<ApiResponse<PagedResult<ReportDto>>> GetAllAsync(int page, int pageSize, string? status)
    {
        var query = _db.Reports.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();
        var ids = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => r.Id)
            .ToListAsync();

        var items = new List<ReportDto>();
        foreach (var id in ids)
            items.Add(await MapReportAsync(id));

        return ApiResponse<PagedResult<ReportDto>>.Ok(new PagedResult<ReportDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApiResponse<ReportDto>> AdminActionAsync(int id, AdminReportActionRequest req, int adminId)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report is null) return ApiResponse<ReportDto>.Fail("Rapò pa jwenn.");
        if (report.Status != "Pending")
            return ApiResponse<ReportDto>.Fail("Rapò sa a deja trete.");

        report.Status = req.Action;
        report.AdminNote = req.Note;
        report.ReviewedByAdminId = adminId;
        report.ReviewedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ApiResponse<ReportDto>.Ok(
            await MapReportAsync(id),
            $"Rapò {(req.Action == "Reviewed" ? "trete" : "anile")} avèk siksè.");
    }

    private async Task<ReportDto> MapReportAsync(int id)
    {
        var r = await _db.Reports
            .Include(x => x.Reporter)
            .Include(x => x.House)
            .FirstAsync(x => x.Id == id);

        return new ReportDto
        {
            Id = r.Id,
            HouseId = r.HouseId,
            HouseTitre = r.House.Titre,
            ReporterNom = r.Reporter.Nom,
            Reason = r.Reason,
            Details = r.Details,
            Status = r.Status,
            AdminNote = r.AdminNote,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt
        };
    }
}

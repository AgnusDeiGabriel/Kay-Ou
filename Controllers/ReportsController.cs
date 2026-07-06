using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayAyiti.DTOs;
using KayAyiti.Services;
using System.Security.Claims;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Rapòte yon kay ki sanble frodilè</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest req)
    {
        var result = await _reports.CreateAsync(req, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Admin: lis tout rapò yo</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        var result = await _reports.GetAllAsync(page, pageSize, status);
        return Ok(result);
    }

    /// <summary>Admin: trete oswa anile yon rapò</summary>
    [HttpPatch("admin/{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(int id, [FromBody] AdminReportActionRequest req)
    {
        var result = await _reports.AdminActionAsync(id, req, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

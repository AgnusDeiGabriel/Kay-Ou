using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Helpers;
using KayAyiti.Services;
using System.Security.Claims;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IKycService _kyc;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public KycController(IKycService kyc, AppDbContext db, IWebHostEnvironment env)
    {
        _kyc = kyc;
        _db = db;
        _env = env;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Soumèt dokiman idantite (kat idantite/paspò + selfie)</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(IFormFile idDocument, IFormFile selfie)
    {
        var result = await _kyc.SubmitAsync(CurrentUserId, idDocument, selfie);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Estati verifikasyon idantite itilizatè konekte a</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var result = await _kyc.GetStatusAsync(CurrentUserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ─── Admin ───────────────────────────────────────────────────────────────

    /// <summary>Admin: lis tout soumisyon KYC yo</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        var result = await _kyc.GetQueueAsync(page, pageSize, status);
        return Ok(result);
    }

    /// <summary>Admin: verifye oswa rejte idantite yon itilizatè</summary>
    [HttpPatch("admin/{userId}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(int userId, [FromBody] AdminKycActionRequest req)
    {
        var result = await _kyc.AdminActionAsync(userId, req, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Admin: wè dokiman idantite (prive)</summary>
    [HttpGet("admin/{userId}/document")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ViewDocument(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return PrivateFileHelper.Serve(_env, "kyc-id", user?.IdDocumentPath);
    }

    /// <summary>Admin: wè selfie (prive)</summary>
    [HttpGet("admin/{userId}/selfie")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ViewSelfie(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return PrivateFileHelper.Serve(_env, "kyc-selfie", user?.SelfiePath);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayAyiti.DTOs;
using KayAyiti.Services;
using System.Security.Claims;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>Inisye peman — upload resi MonCash/NatCash</summary>
    [HttpPost("{houseId}")]
    public async Task<IActionResult> Pay(int houseId, IFormFile resi, [FromForm] string? paymentMethod, [FromForm] string? telefon)
    {
        if (resi is null)
            return BadRequest(ApiResponse<bool>.Fail("Ou dwe upload resi peman an."));

        var result = await _payments.InitiateAsync(houseId, resi, CurrentUserId, paymentMethod);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Wè peman mwen yo</summary>
    [HttpGet("my")]
    public async Task<IActionResult> MyPayments()
    {
        var result = await _payments.GetMyPaymentsAsync(CurrentUserId);
        return Ok(result);
    }

    // ─── Admin ───────────────────────────────────────────────────────────────

    /// <summary>Admin: wè tout peman avèk filtre</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? statut = null)
    {
        var result = await _payments.GetAllAsync(page, pageSize, statut);
        return Ok(result);
    }

    /// <summary>Admin: verifye oswa rejte yon peman</summary>
    [HttpPatch("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewPayment(int id, [FromBody] AdminPaymentActionRequest req)
    {
        var result = await _payments.AdminActionAsync(id, req, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
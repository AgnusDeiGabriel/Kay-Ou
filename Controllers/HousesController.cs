using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayAyiti.DTOs;
using KayAyiti.Services;
using System.Security.Claims;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/houses")]
public class HousesController : ControllerBase
{
    private readonly IHouseService _houses;

    public HousesController(IHouseService houses) => _houses = houses;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : null;

    private string CurrentRole =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

    /// <summary>Lis tout kay (approved) avèk filtre ak paginasyon</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] HouseFilter filter)
    {
        // Non-admin pa ka filtre pa Statut
        if (CurrentRole != "Admin") filter.Statut = "Approved";
        var result = await _houses.GetAllAsync(filter, CurrentUserId, CurrentRole == "Admin");
        return Ok(result);
    }

    /// <summary>Yon sèl kay pa ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _houses.GetByIdAsync(id, CurrentUserId, CurrentRole == "Admin");
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Kreye nouvo kay (itilizatè konekte)</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateHouseRequest req)
    {
        var result = await _houses.CreateAsync(req, CurrentUserId!.Value);
        return result.Success ? Created($"/api/houses/{result.Data?.Id}", result) : BadRequest(result);
    }

    /// <summary>Modifye kay (sèlman pwopriyetè a)</summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHouseRequest req)
    {
        var result = await _houses.UpdateAsync(id, req, CurrentUserId!.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Efase kay</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _houses.DeleteAsync(id, CurrentUserId!.Value, CurrentRole);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload foto pou yon kay</summary>
    [HttpPost("{id}/images")]
    [Authorize]
    public async Task<IActionResult> UploadImages(int id, [FromForm] IFormFileCollection files)
    {
        if (files.Count == 0)
            return BadRequest(ApiResponse<bool>.Fail("Ou dwe voye omwen yon imaj."));
        if (files.Count > 10)
            return BadRequest(ApiResponse<bool>.Fail("Maksimòm 10 imaj pou yon sèl kay."));

        var result = await _houses.UploadImagesAsync(id, files, CurrentUserId!.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Kay aktyèl itilizatè a (tout statut)</summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMy()
    {
        var result = await _houses.GetMyHousesAsync(CurrentUserId!.Value);
        return Ok(result);
    }

    // ─── Admin Endpoints ─────────────────────────────────────────────────────

    /// <summary>Admin: wè tout kay (nenpòt statut)</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminGetAll([FromQuery] HouseFilter filter)
    {
        var result = await _houses.GetAllAsync(filter, CurrentUserId, isAdmin: true);
        return Ok(result);
    }

    /// <summary>Admin: apwouve oswa rejte yon kay</summary>
    [HttpPatch("{id}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReviewHouse(int id, [FromBody] AdminHouseActionRequest req)
    {
        var result = await _houses.AdminActionAsync(id, req, CurrentUserId!.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
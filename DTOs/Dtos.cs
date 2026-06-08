using System.ComponentModel.DataAnnotations;

namespace KayAyiti.DTOs;

// ─── AUTH ───────────────────────────────────────────────────────────────────

public class RegisterRequest
{
    [Required(ErrorMessage = "Non obligatwa")]
    [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email obligatwa")]
    [EmailAddress(ErrorMessage = "Email pa valid")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefòn obligatwa")]
    [MaxLength(20)]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Modpas obligatwa")]
    [MinLength(8, ErrorMessage = "Modpas dwe gen omwen 8 karaktè")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class GoogleTokenRequest
{
    [Required] public string Credential { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

// ─── USER ────────────────────────────────────────────────────────────────────

public class UserDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? HouseCount { get; set; }
    public int? PaymentCount { get; set; }
}

// ─── HOUSE ───────────────────────────────────────────────────────────────────

public class CreateHouseRequest
{
    [Required(ErrorMessage = "Tit obligatwa")]
    [MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Deskripsyon obligatwa")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(1, 99999999, ErrorMessage = "Pri dwe plis pase 0")]
    public decimal Prix { get; set; }

    [Required(ErrorMessage = "Zòn obligatwa")]
    public string Zone { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Vann|Lwe)$", ErrorMessage = "Tip dwe 'Vann' oswa 'Lwe'")]
    public string Type { get; set; } = string.Empty;

    [Range(1, 20)] public int Chambres { get; set; }
    [Range(1, 10)] public int Beny { get; set; }
    [Range(10, 10000)] public int Superficie { get; set; }
    public bool Garaj { get; set; }
    public bool Sekirite { get; set; }
    public bool Piscine { get; set; }
}

public class UpdateHouseRequest
{
    [MaxLength(200)] public string? Titre { get; set; }
    public string? Description { get; set; }
    public decimal? Prix { get; set; }
    public string? Zone { get; set; }
    public string? Type { get; set; }
    public int? Chambres { get; set; }
    public int? Beny { get; set; }
    public int? Superficie { get; set; }
    public bool? Garaj { get; set; }
    public bool? Sekirite { get; set; }
    public bool? Piscine { get; set; }
}

public class HouseDto
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    public string Zone { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Chambres { get; set; }
    public int Beny { get; set; }
    public int Superficie { get; set; }
    public bool Garaj { get; set; }
    public bool Sekirite { get; set; }
    public bool Piscine { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto Owner { get; set; } = null!;
    public List<string> Images { get; set; } = new();
    public bool UserHasPaid { get; set; }
}

public class AdminHouseActionRequest
{
    [Required]
    [RegularExpression("^(Approved|Rejected)$")]
    public string Action { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

// ─── PAYMENT ─────────────────────────────────────────────────────────────────

public class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? ResiImageUrl { get; set; }
    public string? NoteAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public UserDto User { get; set; } = null!;
    public HouseDto House { get; set; } = null!;
}

public class AdminPaymentActionRequest
{
    [Required]
    [RegularExpression("^(Verified|Rejected)$")]
    public string Action { get; set; } = string.Empty;

    public string? NoteAdmin { get; set; }
}

// ─── PAGINATION ──────────────────────────────────────────────────────────────

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class HouseFilter
{
    public string? Zone { get; set; }
    public string? Type { get; set; }
    public string? Statut { get; set; }
    public decimal? PrixMin { get; set; }
    public decimal? PrixMax { get; set; }
    public int? ChambreMin { get; set; }
    public int? ChambreMax { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}

// ─── GENERIC ─────────────────────────────────────────────────────────────────

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Siksè") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}
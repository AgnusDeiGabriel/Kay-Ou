using System.ComponentModel.DataAnnotations;

namespace KayAyiti.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Telefon { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // "User" | "Admin"

    public bool IsActive { get; set; } = true;

    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationExpiry { get; set; }

    public string? AvatarUrl { get; set; }

    // ─── Verifikasyon idantite (KYC) ("None" | "Pending" | "Verified" | "Rejected") ───
    public string KycStatus { get; set; } = "None";
    public string? KycRejectionReason { get; set; }
    public DateTime? KycSubmittedAt { get; set; }
    public DateTime? KycVerifiedAt { get; set; }
    public int? KycVerifiedByAdminId { get; set; }
    public string? IdDocumentPath { get; set; }
    public string? SelfiePath { get; set; }

    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    public string? AppleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<House> Houses { get; set; } = new List<House>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayAyiti.Models;

public class Payment
{
    public int Id { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 1500; // HTG

    // "Pending" | "Verified" | "Rejected"
    public string Statut { get; set; } = "Pending";

    public string? ResiImageUrl { get; set; }   // foto resi MonCash/NatCash
    public string? PaymentMethod { get; set; } // "MonCash" | "NatCash"
    public string? NoteAdmin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }

    // Foreign keys
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int HouseId { get; set; }
    public House House { get; set; } = null!;

    public int? VerifiedByAdminId { get; set; }
}

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
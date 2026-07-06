using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayAyiti.Models;

public class House
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Prix { get; set; }

    [Required, MaxLength(100)]
    public string Zone { get; set; } = string.Empty;

    // "Vann" | "Lwe"
    [Required, MaxLength(10)]
    public string Type { get; set; } = string.Empty;

    public int Chambres { get; set; }
    public int Beny { get; set; }
    public int Superficie { get; set; } // m²

    public bool Garaj { get; set; }
    public bool Sekirite { get; set; }
    public bool Piscine { get; set; }

    // "Pending" | "Approved" | "Rejected"
    public string Statut { get; set; } = "Pending";

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    // ─── Verifikasyon pwopriyete ("None" | "Pending" | "Verified" | "Rejected") ───
    public string OwnershipStatus { get; set; } = "None";
    public string? OwnershipRejectionReason { get; set; }
    public DateTime? OwnershipSubmittedAt { get; set; }
    public DateTime? OwnershipVerifiedAt { get; set; }
    public int? OwnershipVerifiedByAdminId { get; set; }

    public string? OwnershipTitleDeedPath { get; set; }
    public string? OwnershipMandatePath { get; set; }
    public string? OwnershipTaxReceiptPath { get; set; }

    // Done revizyon admin sèlman — pa itilize pou kat piblik la (wwwroot/map.js)
    public string? AdresseComplete { get; set; }
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }
    [Column(TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    // Foreign key
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation
    public ICollection<HouseImage> Images { get; set; } = new List<HouseImage>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<HouseLike> Likes { get; set; } = new List<HouseLike>();
}

public class HouseImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrincipal { get; set; } = false;
    public int HouseId { get; set; }
    public House House { get; set; } = null!;
}
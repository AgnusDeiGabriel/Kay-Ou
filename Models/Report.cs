namespace KayAyiti.Models;

public class Report
{
    public int Id { get; set; }

    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public int HouseId { get; set; }
    public House House { get; set; } = null!;

    // "FauKay" | "SispisyonFwod" | "DejaVann" | "KontniInapwopriye" | "Lòt"
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    // "Pending" | "Reviewed" | "Dismissed"
    public string Status { get; set; } = "Pending";
    public string? AdminNote { get; set; }
    public int? ReviewedByAdminId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

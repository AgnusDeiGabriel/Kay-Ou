namespace KayAyiti.Models;

public class HouseLike
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int HouseId { get; set; }
    public House House { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace KayAyiti.Models;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Type { get; set; } = ""; // "house" | "payment" | "message"
    public bool IsRead { get; set; } = false;
    public int? RelatedId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

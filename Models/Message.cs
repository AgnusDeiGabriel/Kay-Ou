namespace KayAyiti.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
    public bool IsPinned { get; set; } = false;
    public string? AudioUrl { get; set; }

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public int ReceiverId { get; set; }
    public User Receiver { get; set; } = null!;

    public int HouseId { get; set; }
    public House House { get; set; } = null!;
}

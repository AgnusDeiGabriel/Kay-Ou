using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayAyiti.Data;
using KayAyiti.DTOs;
using KayAyiti.Models;
using System.Security.Claims;

namespace KayAyiti.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChatController(AppDbContext db) => _db = db;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/chat/{houseId}
    // Visitor: returns conversation between current user and owner
    // Owner:   requires ?with={visitorId}
    [HttpGet("{houseId}")]
    public async Task<IActionResult> GetMessages(int houseId, [FromQuery] int? with)
    {
        var userId = GetUserId();
        var house = await _db.Houses
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.Id == houseId);

        if (house == null)
            return NotFound(ApiResponse<object>.Fail("Kay pa jwenn."));

        int visitorId, ownerId = house.UserId;

        if (userId == ownerId)
        {
            if (with == null)
                return BadRequest(ApiResponse<object>.Fail("Mete ?with={userId} pou wè konvèsasyon an."));
            visitorId = with.Value;
        }
        else
        {
            visitorId = userId;
        }

        // Mark received messages as read
        var unread = await _db.Messages
            .Where(m => m.HouseId == houseId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();
        if (unread.Count > 0)
        {
            unread.ForEach(m => m.IsRead = true);
            await _db.SaveChangesAsync();
        }

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.HouseId == houseId &&
                       ((m.SenderId == visitorId && m.ReceiverId == ownerId) ||
                        (m.SenderId == ownerId   && m.ReceiverId == visitorId)))
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto
            {
                Id        = m.Id,
                Content   = m.Content,
                SentAt    = m.SentAt,
                IsRead    = m.IsRead,
                IsMine    = m.SenderId == userId,
                SenderNom = m.Sender.Nom,
                IsEdited  = m.IsEdited,
                EditedAt  = m.EditedAt,
                IsPinned        = m.IsPinned,
                AudioUrl        = m.AudioUrl,
                SenderAvatarUrl = m.Sender.AvatarUrl
            })
            .ToListAsync();

        var pinned = messages.FirstOrDefault(m => m.IsPinned);

        return Ok(ApiResponse<object>.Ok(new
        {
            messages,
            ownerNom      = house.User.Nom,
            houseTitre    = house.Titre,
            ownerId       = house.UserId,
            pinnedMessage = pinned
        }));
    }

    // POST /api/chat/{houseId}
    [HttpPost("{houseId}")]
    public async Task<IActionResult> SendMessage(
        int houseId,
        [FromBody] SendMessageRequest req,
        [FromQuery] int? with)
    {
        var userId = GetUserId();
        var house = await _db.Houses.FirstOrDefaultAsync(h => h.Id == houseId);

        if (house == null)
            return NotFound(ApiResponse<object>.Fail("Kay pa jwenn."));

        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(ApiResponse<object>.Fail("Mesaj la pa ka vid."));

        int receiverId;
        if (userId == house.UserId)
        {
            if (with == null)
                return BadRequest(ApiResponse<object>.Fail("Mete ?with={userId}."));
            // Confirm conversation exists before owner can reply
            var exists = await _db.Messages.AnyAsync(m => m.HouseId == houseId && m.SenderId == with.Value);
            if (!exists)
                return BadRequest(ApiResponse<object>.Fail("Konvèsasyon pa egziste."));
            receiverId = with.Value;
        }
        else
        {
            // Visitor cannot send to themselves
            if (userId == house.UserId)
                return BadRequest(ApiResponse<object>.Fail("Ou pa ka voye mesaj bay pwòp kay ou."));
            receiverId = house.UserId;
        }

        var msg = new Message
        {
            Content    = req.Content.Trim(),
            SenderId   = userId,
            ReceiverId = receiverId,
            HouseId    = houseId
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        await _db.Entry(msg).Reference(m => m.Sender).LoadAsync();

        return Ok(ApiResponse<MessageDto>.Ok(new MessageDto
        {
            Id        = msg.Id,
            Content   = msg.Content,
            SentAt    = msg.SentAt,
            IsRead    = false,
            IsMine    = true,
            SenderNom = msg.Sender.Nom
        }));
    }

    // POST /api/chat/{houseId}/voice
    [HttpPost("{houseId}/voice")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SendVoiceMessage(
        int houseId, IFormFile audio, [FromQuery] int? with)
    {
        var userId = GetUserId();
        var house  = await _db.Houses.FirstOrDefaultAsync(h => h.Id == houseId);
        if (house == null) return NotFound(ApiResponse<object>.Fail("Kay pa jwenn."));
        if (audio == null || audio.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Fichye son an vid."));
        if (audio.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("Son twò gwo (max 10 MB)."));

        int receiverId;
        if (userId == house.UserId)
        {
            if (with == null)
                return BadRequest(ApiResponse<object>.Fail("Mete ?with={userId}."));
            var exists = await _db.Messages.AnyAsync(m => m.HouseId == houseId && m.SenderId == with.Value);
            if (!exists) return BadRequest(ApiResponse<object>.Fail("Konvèsasyon pa egziste."));
            receiverId = with.Value;
        }
        else { receiverId = house.UserId; }

        var ext = Path.GetExtension(audio.FileName).ToLowerInvariant();
        if (!new[] { ".webm", ".ogg", ".mp4", ".m4a", ".wav", ".opus" }.Contains(ext)) ext = ".webm";
        var fileName = $"{Guid.NewGuid()}{ext}";
        var dir = Path.Combine("wwwroot", "uploads", "voice");
        Directory.CreateDirectory(dir);
        await using var fs = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
        await audio.CopyToAsync(fs);

        var audioUrl = $"/uploads/voice/{fileName}";
        var msg = new Message
        {
            Content = "", AudioUrl = audioUrl,
            SenderId = userId, ReceiverId = receiverId, HouseId = houseId
        };
        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
        await _db.Entry(msg).Reference(m => m.Sender).LoadAsync();

        return Ok(ApiResponse<MessageDto>.Ok(new MessageDto
        {
            Id = msg.Id, Content = "", AudioUrl = audioUrl,
            SentAt = msg.SentAt, IsRead = false, IsMine = true, SenderNom = msg.Sender.Nom
        }));
    }

    // DELETE /api/chat/{houseId}/{messageId}
    [HttpDelete("{houseId}/{messageId}")]
    public async Task<IActionResult> DeleteMessage(int houseId, int messageId)
    {
        var userId = GetUserId();
        var msg = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.HouseId == houseId);

        if (msg == null)
            return NotFound(ApiResponse<object>.Fail("Mesaj pa jwenn."));
        if (msg.SenderId != userId)
            return StatusCode(403, ApiResponse<object>.Fail("Ou pa ka efase mesaj moun alòt."));

        _db.Messages.Remove(msg);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Mesaj efase."));
    }

    // PATCH /api/chat/{houseId}/{messageId}/pin
    [HttpPatch("{houseId}/{messageId}/pin")]
    public async Task<IActionResult> PinMessage(int houseId, int messageId)
    {
        var userId = GetUserId();
        var msg = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.HouseId == houseId);

        if (msg == null)
            return NotFound(ApiResponse<object>.Fail("Mesaj pa jwenn."));
        if (msg.SenderId != userId && msg.ReceiverId != userId)
            return StatusCode(403, ApiResponse<object>.Fail("Ou pa nan konvèsasyon sa."));

        if (msg.IsPinned)
        {
            msg.IsPinned = false;
        }
        else
        {
            // Dezepingle tout lòt mesaj nan menm konvèsasyon an
            var sA = msg.SenderId; var sB = msg.ReceiverId;
            var others = await _db.Messages
                .Where(m => m.HouseId == houseId && m.IsPinned &&
                    ((m.SenderId == sA && m.ReceiverId == sB) ||
                     (m.SenderId == sB && m.ReceiverId == sA)))
                .ToListAsync();
            others.ForEach(m => m.IsPinned = false);
            msg.IsPinned = true;
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { isPinned = msg.IsPinned }));
    }

    // PATCH /api/chat/{houseId}/{messageId}
    [HttpPatch("{houseId}/{messageId}")]
    public async Task<IActionResult> EditMessage(int houseId, int messageId, [FromBody] EditMessageRequest req)
    {
        var userId = GetUserId();
        var msg = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.HouseId == houseId);

        if (msg == null)
            return NotFound(ApiResponse<object>.Fail("Mesaj pa jwenn."));
        if (msg.SenderId != userId)
            return StatusCode(403, ApiResponse<object>.Fail("Ou pa ka modifye mesaj moun alòt."));
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(ApiResponse<object>.Fail("Mesaj la pa ka vid."));

        msg.Content  = req.Content.Trim();
        msg.IsEdited = true;
        msg.EditedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { id = msg.Id, content = msg.Content }));
    }

    // GET /api/chat/conversations
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = GetUserId();

        var messages = await _db.Messages
            .Include(m => m.House)
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        var conversations = messages
            .GroupBy(m => (m.HouseId, OtherUserId: m.SenderId == userId ? m.ReceiverId : m.SenderId))
            .Select(g =>
            {
                var last      = g.First();
                var otherUser = last.SenderId == userId ? last.Receiver : last.Sender;
                return new ConversationDto
                {
                    HouseId              = g.Key.HouseId,
                    HouseTitre           = last.House.Titre,
                    OtherUserId          = g.Key.OtherUserId,
                    OtherUserNom         = otherUser.Nom,
                    OtherUserAvatarUrl   = otherUser.AvatarUrl,
                    LastMessage          = last.Content,
                    LastMessageAt        = last.SentAt,
                    UnreadCount          = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                    IsLastMessageMine    = last.SenderId == userId,
                    IsCurrentUserOwner   = last.House.UserId == userId
                };
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();

        return Ok(ApiResponse<List<ConversationDto>>.Ok(conversations));
    }
}

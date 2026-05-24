using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatGroupId { get; set; }
    public ChatGroup ChatGroup { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    [MaxLength(2000)] public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

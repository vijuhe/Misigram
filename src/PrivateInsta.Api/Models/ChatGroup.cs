using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.Models;

public class ChatGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatGroupMember> Members { get; set; } = new List<ChatGroupMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(255)] public string GoogleId { get; set; } = string.Empty;
    [MaxLength(255)] public string Email { get; set; } = string.Empty;
    [MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    [MaxLength(500)] public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<ChatGroupMember> ChatGroupMemberships { get; set; } = new List<ChatGroupMember>();
    public ICollection<Message> SentMessages { get; set; } = new List<Message>();
}

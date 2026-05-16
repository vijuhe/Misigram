namespace PrivateInsta.Api.Models;

public class ChatGroupMember
{
    public Guid ChatGroupId { get; set; }
    public ChatGroup ChatGroup { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

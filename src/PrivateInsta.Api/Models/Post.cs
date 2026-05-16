namespace PrivateInsta.Api.Models;

public enum MediaType { Photo, Video }

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public MediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}

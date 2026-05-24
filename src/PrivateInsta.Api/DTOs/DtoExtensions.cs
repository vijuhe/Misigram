using PrivateInsta.Api.Models;
using PrivateInsta.Api.Services;

namespace PrivateInsta.Api.DTOs;

public static class DtoExtensions
{
    public static UserDto ToDto(this User u, BlobStorageService blob) =>
        new(u.Id, u.Email, u.DisplayName, blob.ResolveSasUrl(u.AvatarUrl), u.Bio, u.CreatedAt);

    public static PostDto ToDto(this Post p, BlobStorageService blob, Guid currentUserId) =>
        new(p.Id,
            p.User.ToDto(blob),
            blob.ResolveSasUrl(p.MediaUrl),
            blob.ResolveSasUrl(p.ThumbnailUrl),
            p.MediaType,
            p.Caption,
            p.Likes.Count,
            p.Comments.Count,
            p.Likes.Any(l => l.UserId == currentUserId),
            p.CreatedAt);

    public static StoryDto ToDto(this Story s, BlobStorageService blob) =>
        new(s.Id, s.User.ToDto(blob), blob.ResolveSasUrl(s.MediaUrl), s.MediaType, s.CreatedAt, s.ExpiresAt);

    public static MessageDto ToDto(this Message m, BlobStorageService blob) =>
        new(m.Id, m.ChatGroupId, m.Sender.ToDto(blob), m.Content, blob.ResolveSasUrl(m.MediaUrl), m.CreatedAt);

    public static ChatGroupDto ToDto(this ChatGroup g, BlobStorageService blob)
    {
        var last = g.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        return new(g.Id, g.Name, g.Members.Select(m => m.User.ToDto(blob)), last?.ToDto(blob), g.CreatedAt);
    }
}

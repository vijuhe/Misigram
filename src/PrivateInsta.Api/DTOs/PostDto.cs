using PrivateInsta.Api.Models;

namespace PrivateInsta.Api.DTOs;

public record PostDto(
    Guid Id,
    UserDto Author,
    string? MediaUrl,
    string? ThumbnailUrl,
    MediaType MediaType,
    string? Caption,
    int LikeCount,
    int CommentCount,
    bool LikedByMe,
    DateTime CreatedAt);

public record CreatePostRequest(string? Caption);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);

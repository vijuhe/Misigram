using PrivateInsta.Api.Models;

namespace PrivateInsta.Api.DTOs;

public record StoryDto(Guid Id, UserDto Author, string? MediaUrl, MediaType MediaType, DateTime CreatedAt, DateTime ExpiresAt);

public record UserStoriesDto(UserDto User, IEnumerable<StoryDto> Stories);

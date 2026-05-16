namespace PrivateInsta.Api.DTOs;

public record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl, string? Bio, DateTime CreatedAt);

public record UpdateProfileRequest(string DisplayName, string? Bio);

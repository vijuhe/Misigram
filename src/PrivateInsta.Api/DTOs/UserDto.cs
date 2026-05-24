using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.DTOs;

public record UserDto(Guid Id, string Email, string DisplayName, string? AvatarUrl, string? Bio, DateTime CreatedAt);

public record UpdateProfileRequest(
    [Required][MaxLength(100)] string DisplayName,
    [MaxLength(500)] string? Bio);

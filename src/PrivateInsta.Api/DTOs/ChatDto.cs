using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.DTOs;

public record ChatGroupDto(Guid Id, string? Name, IEnumerable<UserDto> Members, MessageDto? LastMessage, DateTime CreatedAt);

public record MessageDto(Guid Id, Guid ChatGroupId, UserDto Sender, string Content, string? MediaUrl, DateTime CreatedAt);

public record CreateChatRequest([MaxLength(100)] string? Name, IEnumerable<Guid> MemberIds);

public record SendMessageRequest([Required][MaxLength(2000)] string Content, string? MediaUrl);

public record AddMemberRequest(Guid UserId);

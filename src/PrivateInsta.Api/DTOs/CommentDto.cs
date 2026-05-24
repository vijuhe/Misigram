using System.ComponentModel.DataAnnotations;

namespace PrivateInsta.Api.DTOs;

public record CommentDto(Guid Id, UserDto Author, string Content, DateTime CreatedAt);

public record CreateCommentRequest([Required][MaxLength(2200)] string Content);

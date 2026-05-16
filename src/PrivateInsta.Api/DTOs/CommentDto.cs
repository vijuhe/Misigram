namespace PrivateInsta.Api.DTOs;

public record CommentDto(Guid Id, UserDto Author, string Content, DateTime CreatedAt);

public record CreateCommentRequest(string Content);

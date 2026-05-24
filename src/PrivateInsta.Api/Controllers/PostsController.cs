using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrivateInsta.Api.Data;
using PrivateInsta.Api.DTOs;
using PrivateInsta.Api.Models;
using PrivateInsta.Api.Services;

namespace PrivateInsta.Api.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController(AppDbContext db, BlobStorageService blob) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<PostDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var total = await db.Posts.CountAsync();
        var posts = await db.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var me = CurrentUserId;
        var dtos = posts.Select(p => ToDto(p, me));
        return Ok(new PagedResult<PostDto>(dtos, total, page, size));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDto>> GetById(Guid id)
    {
        var post = await db.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id);

        return post is null ? NotFound() : Ok(ToDto(post, CurrentUserId));
    }

    [HttpPost]
    [RequestSizeLimit(104_857_600)] // 100 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    public async Task<ActionResult<PostDto>> Create([FromForm] string? caption, IFormFile? file)
    {
        if (file is null && string.IsNullOrWhiteSpace(caption))
            return BadRequest("A caption is required when posting without media.");

        var postId = Guid.NewGuid();
        string? mediaUrl = null, thumbUrl = null;
        var mediaType = MediaType.Photo;

        if (file is { Length: > 0 })
        {
            var isVideo = file.ContentType.StartsWith("video/");
            var ext = Path.GetExtension(file.FileName);
            mediaType = isVideo ? MediaType.Video : MediaType.Photo;

            using var stream = file.OpenReadStream();
            if (isVideo)
            {
                mediaUrl = await blob.UploadAsync(stream, $"posts/{postId}/original{ext}", file.ContentType);
                thumbUrl = mediaUrl;
            }
            else
            {
                thumbUrl = await blob.UploadWithThumbnailAsync(stream, $"posts/{postId}", ext, file.ContentType);
                mediaUrl = $"posts/{postId}/original{ext}";
            }
        }

        var post = new Post
        {
            Id = postId,
            UserId = CurrentUserId,
            MediaUrl = mediaUrl,
            ThumbnailUrl = thumbUrl,
            MediaType = mediaType,
            Caption = caption
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        await db.Entry(post).Reference(p => p.User).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, ToDto(post, CurrentUserId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return NotFound();
        if (post.UserId != CurrentUserId) return Forbid();

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(Guid id)
    {
        var comments = await db.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == id)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(c => new CommentDto(c.Id, UserToDto(c.User), c.Content, c.CreatedAt)));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(Guid id, [FromBody] CreateCommentRequest req)
    {
        if (!await db.Posts.AnyAsync(p => p.Id == id)) return NotFound();

        var comment = new Comment { PostId = id, UserId = CurrentUserId, Content = req.Content };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        await db.Entry(comment).Reference(c => c.User).LoadAsync();

        return CreatedAtAction(nameof(GetComments), new { id },
            new CommentDto(comment.Id, UserToDto(comment.User), comment.Content, comment.CreatedAt));
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment is null || comment.PostId != id) return NotFound();
        if (comment.UserId != CurrentUserId) return Forbid();

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<object>> ToggleLike(Guid id)
    {
        if (!await db.Posts.AnyAsync(p => p.Id == id)) return NotFound();

        var existing = await db.Likes.FirstOrDefaultAsync(l => l.PostId == id && l.UserId == CurrentUserId);
        bool liked;

        if (existing is not null)
        {
            db.Likes.Remove(existing);
            liked = false;
        }
        else
        {
            db.Likes.Add(new Like { PostId = id, UserId = CurrentUserId });
            liked = true;
        }

        await db.SaveChangesAsync();
        return Ok(new { liked });
    }

    private PostDto ToDto(Post p, Guid me) => new(
        p.Id,
        UserToDto(p.User),
        blob.ResolveSasUrl(p.MediaUrl),
        blob.ResolveSasUrl(p.ThumbnailUrl),
        p.MediaType,
        p.Caption,
        p.Likes.Count,
        p.Comments.Count,
        p.Likes.Any(l => l.UserId == me),
        p.CreatedAt);

    private UserDto UserToDto(Models.User u) =>
        new(u.Id, u.Email, u.DisplayName, blob.ResolveSasUrl(u.AvatarUrl), u.Bio, u.CreatedAt);
}

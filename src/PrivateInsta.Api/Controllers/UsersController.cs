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
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext db, BlobStorageService blob) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await db.Users.OrderBy(u => u.DisplayName).ToListAsync();
        return Ok(users.Select(u => u.ToDto(blob)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        return user is null ? NotFound() : Ok(user.ToDto(blob));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        return user is null ? Unauthorized() : Ok(user.ToDto(blob));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();

        user.DisplayName = req.DisplayName;
        user.Bio = req.Bio;
        await db.SaveChangesAsync();
        return Ok(user.ToDto(blob));
    }

    [HttpGet("{id:guid}/posts")]
    public async Task<ActionResult<PagedResult<PostDto>>> GetPosts(Guid id, [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var me = CurrentUserId;
        var total = await db.Posts.CountAsync(p => p.UserId == id);
        var posts = await db.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Where(p => p.UserId == id)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return Ok(new PagedResult<PostDto>(posts.Select(p => p.ToDto(blob, me)), total, page, size));
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(2_097_152)] // 2 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 2_097_152)]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file)
    {
        if (file.Length == 0) return BadRequest("No file provided.");

        var user = await db.Users.FindAsync(CurrentUserId);
        if (user is null) return Unauthorized();

        using var stream = file.OpenReadStream();
        user.AvatarUrl = await blob.UploadAsync(stream, $"avatars/{user.Id}.jpg", "image/jpeg");
        await db.SaveChangesAsync();
        return Ok(user.ToDto(blob));
    }
}

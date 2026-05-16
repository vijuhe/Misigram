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
[Route("api/stories")]
[Authorize]
public class StoriesController(AppDbContext db, BlobStorageService blob) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserStoriesDto>>> GetActive()
    {
        var stories = await db.Stories
            .Include(s => s.User)
            .Where(s => s.ExpiresAt > DateTime.UtcNow)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var grouped = stories
            .GroupBy(s => s.User)
            .Select(g => new UserStoriesDto(
                UserToDto(g.Key),
                g.Select(s => new StoryDto(s.Id, UserToDto(s.User), blob.ResolveSasUrl(s.MediaUrl), s.MediaType, s.CreatedAt, s.ExpiresAt))));

        return Ok(grouped);
    }

    [HttpPost]
    public async Task<ActionResult<StoryDto>> Create(IFormFile file)
    {
        if (file.Length == 0) return BadRequest("No file provided.");

        var storyId = Guid.NewGuid();
        var isVideo = file.ContentType.StartsWith("video/");
        var ext = Path.GetExtension(file.FileName);

        using var stream = file.OpenReadStream();
        var url = await blob.UploadAsync(stream, $"stories/{storyId}/original{ext}", file.ContentType);

        var story = new Story
        {
            Id = storyId,
            UserId = CurrentUserId,
            MediaUrl = url,
            MediaType = isVideo ? MediaType.Video : MediaType.Photo
        };

        db.Stories.Add(story);
        await db.SaveChangesAsync();
        await db.Entry(story).Reference(s => s.User).LoadAsync();

        return CreatedAtAction(nameof(GetActive), null,
            new StoryDto(story.Id, UserToDto(story.User), blob.ResolveSasUrl(story.MediaUrl), story.MediaType, story.CreatedAt, story.ExpiresAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var story = await db.Stories.FindAsync(id);
        if (story is null) return NotFound();
        if (story.UserId != CurrentUserId) return Forbid();

        await blob.DeleteAsync($"stories/{story.Id}");
        db.Stories.Remove(story);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private UserDto UserToDto(Models.User u) =>
        new(u.Id, u.Email, u.DisplayName, blob.ResolveSasUrl(u.AvatarUrl), u.Bio, u.CreatedAt);
}

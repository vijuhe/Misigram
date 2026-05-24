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
[Route("api/chats")]
[Authorize]
public class ChatsController(AppDbContext db, BlobStorageService blob) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChatGroupDto>>> GetMyChats()
    {
        var chats = await db.ChatGroups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Messages.OrderByDescending(m => m.CreatedAt).Take(1)).ThenInclude(m => m.Sender)
            .Where(g => g.Members.Any(m => m.UserId == CurrentUserId))
            .OrderByDescending(g => g.Messages.Max(m => (DateTime?)m.CreatedAt) ?? g.CreatedAt)
            .ToListAsync();

        return Ok(chats.Select(g => g.ToDto(blob)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChatGroupDto>> GetById(Guid id)
    {
        if (!await IsMemberAsync(id)) return Forbid();

        var group = await db.ChatGroups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .Include(g => g.Messages.OrderByDescending(m => m.CreatedAt).Take(1)).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(g => g.Id == id);

        return group is null ? NotFound() : Ok(group.ToDto(blob));
    }

    [HttpPost]
    public async Task<ActionResult<ChatGroupDto>> Create([FromBody] CreateChatRequest req)
    {
        var me = CurrentUserId;
        var memberIds = req.MemberIds.Append(me).Distinct().ToList();

        var group = new ChatGroup { Name = req.Name };
        group.Members = memberIds.Select(uid => new ChatGroupMember { ChatGroupId = group.Id, UserId = uid }).ToList();

        db.ChatGroups.Add(group);
        await db.SaveChangesAsync();

        await db.Entry(group).Collection(g => g.Members).Query().Include(m => m.User).LoadAsync();
        return CreatedAtAction(nameof(GetMessages), new { id = group.Id }, group.ToDto(blob));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<PagedResult<MessageDto>>> GetMessages(Guid id, [FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        if (!await IsMemberAsync(id)) return Forbid();

        var total = await db.Messages.CountAsync(m => m.ChatGroupId == id);
        var messages = await db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ChatGroupId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return Ok(new PagedResult<MessageDto>(messages.Select(m => m.ToDto(blob)), total, page, size));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest req)
    {
        if (!await IsMemberAsync(id)) return Forbid();

        if (!await db.Users.AnyAsync(u => u.Id == req.UserId))
            return NotFound("User not found.");

        if (await db.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == id && m.UserId == req.UserId))
            return Conflict("User is already a member.");

        db.ChatGroupMembers.Add(new ChatGroupMember { ChatGroupId = id, UserId = req.UserId });
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<bool> IsMemberAsync(Guid chatId) =>
        await db.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == chatId && m.UserId == CurrentUserId);

}

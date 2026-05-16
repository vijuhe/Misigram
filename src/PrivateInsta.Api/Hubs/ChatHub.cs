using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrivateInsta.Api.Data;
using PrivateInsta.Api.DTOs;
using PrivateInsta.Api.Models;
using PrivateInsta.Api.Services;

namespace PrivateInsta.Api.Hubs;

[Authorize]
public class ChatHub(AppDbContext db, BlobStorageService blob) : Hub
{
    private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task JoinGroup(Guid chatGroupId)
    {
        if (!await IsMemberAsync(chatGroupId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, chatGroupId.ToString());
    }

    public async Task LeaveGroup(Guid chatGroupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatGroupId.ToString());
    }

    public async Task SendMessage(Guid chatGroupId, string content)
    {
        if (!await IsMemberAsync(chatGroupId)) return;

        var message = new Message
        {
            ChatGroupId = chatGroupId,
            SenderId = CurrentUserId,
            Content = content
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();
        await db.Entry(message).Reference(m => m.Sender).LoadAsync();

        var dto = new MessageDto(
            message.Id,
            message.ChatGroupId,
            new UserDto(message.Sender.Id, message.Sender.Email, message.Sender.DisplayName, blob.ResolveSasUrl(message.Sender.AvatarUrl), message.Sender.Bio, message.Sender.CreatedAt),
            message.Content,
            blob.ResolveSasUrl(message.MediaUrl),
            message.CreatedAt);

        await Clients.Group(chatGroupId.ToString()).SendAsync("ReceiveMessage", dto);
    }

    private async Task<bool> IsMemberAsync(Guid chatId) =>
        await db.ChatGroupMembers.AnyAsync(m => m.ChatGroupId == chatId && m.UserId == CurrentUserId);
}

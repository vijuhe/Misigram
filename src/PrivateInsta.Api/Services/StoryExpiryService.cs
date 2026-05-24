using Microsoft.EntityFrameworkCore;
using PrivateInsta.Api.Data;

namespace PrivateInsta.Api.Services;

public class StoryExpiryService(IServiceScopeFactory scopeFactory, ILogger<StoryExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            await PurgeExpiredStoriesAsync(ct);
        }
    }

    private async Task PurgeExpiredStoriesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blob = scope.ServiceProvider.GetRequiredService<BlobStorageService>();

        var expired = await db.Stories
            .Where(s => s.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var story in expired)
        {
            try { if (story.MediaUrl is not null) await blob.DeleteAsync(story.MediaUrl, ct); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete blob for story {Id}", story.Id); }
        }

        db.Stories.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Purged {Count} expired stories", expired.Count);
    }
}

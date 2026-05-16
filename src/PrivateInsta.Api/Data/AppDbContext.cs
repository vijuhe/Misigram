using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateInsta.Api.Models;

namespace PrivateInsta.Api.Data;

// EF Core reads DateTime from SQL Server as Unspecified kind; this converter
// tags every value as UTC so System.Text.Json serializes it with the Z suffix.
file sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<ChatGroup> ChatGroups => Set<ChatGroup>();
    public DbSet<ChatGroupMember> ChatGroupMembers => Set<ChatGroupMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.GoogleId).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Like>(e =>
        {
            e.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
        });

        b.Entity<ChatGroupMember>(e =>
        {
            e.HasKey(m => new { m.ChatGroupId, m.UserId });
        });

        b.Entity<Story>(e =>
        {
            e.HasIndex(s => s.ExpiresAt);
        });

        // SQL Server disallows multiple cascade paths to the same table.
        // Restrict delete on all User FK relationships so cascades only flow
        // from Post → Comments/Likes, not from User → Comments/Likes directly.
        b.Entity<Comment>()
            .HasOne(c => c.User).WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Like>()
            .HasOne(l => l.User).WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Story>()
            .HasOne(s => s.User).WithMany(u => u.Stories)
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Message>()
            .HasOne(m => m.Sender).WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ChatGroupMember>()
            .HasOne(m => m.User).WithMany(u => u.ChatGroupMemberships)
            .HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

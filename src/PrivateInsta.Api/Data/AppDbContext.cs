using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrivateInsta.Api.Models;

namespace PrivateInsta.Api.Data;

// Npgsql requires DateTimeKind.Utc for timestamptz columns; this converter
// enforces it on both read and write so System.Text.Json also serializes with Z.
file sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
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

        // Restrict FK deletes on User so cascade paths don't conflict.
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

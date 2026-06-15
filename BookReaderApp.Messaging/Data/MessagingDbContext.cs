using BookReaderApp.Messaging.Models;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Messaging.Data;

// Dedicated context for the messaging module. It shares the host's physical database but
// keeps its own schema and migrations history table (configured at registration in the
// host's Program.cs) so it never collides with the Identity context.
public class MessagingDbContext : DbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Conversation>(entity =>
        {
            entity.Property(c => c.User1Id).IsRequired();
            entity.Property(c => c.User2Id).IsRequired();

            // Exactly one conversation per normalized participant pair.
            entity.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
        });

        builder.Entity<DirectMessage>(entity =>
        {
            entity.Property(m => m.SenderId).IsRequired();
            entity.Property(m => m.Content).IsRequired();

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Supports thread paging (newest-first within a conversation).
            entity.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });
    }
}

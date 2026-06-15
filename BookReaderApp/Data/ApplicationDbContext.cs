using BookReaderApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<UserBook> UserBooks => Set<UserBook>();

    public DbSet<Shelf> Shelves => Set<Shelf>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();

    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();

    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Required first: lets Identity configure its own schema before we add ours.
        base.OnModelCreating(builder);

        builder.Entity<Book>(entity =>
        {
            entity.HasOne(b => b.Author)
				.WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Genre>();

        builder.Entity<UserBook>(entity =>
        {
            entity.HasOne(ub => ub.Book)
                .WithMany(b => b.UserBooks)
                .HasForeignKey(ub => ub.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ub => ub.User)
                .WithMany(u => u.UserBooks)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional placement on a custom shelf. Restrict: shelves can't be deleted
            // while books still reference them (no shelf delete is in scope anyway).
            entity.HasOne(ub => ub.Shelf)
                .WithMany()
                .HasForeignKey(ub => ub.ShelfId)
                .OnDelete(DeleteBehavior.Restrict);

            // A user can shelve a given book only once.
            entity.HasIndex(ub => new { ub.UserId, ub.BookId }).IsUnique();
        });

        builder.Entity<Shelf>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany(u => u.Shelves)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Shelf names are unique per user.
            entity.HasIndex(s => new { s.UserId, s.Name }).IsUnique();
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.Book)
                .WithMany()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // A user can review a given book only once.
            entity.HasIndex(r => new { r.UserId, r.BookId }).IsUnique();
        });

        builder.Entity<ReviewLike>(entity =>
        {
            entity.HasOne(l => l.Review)
                .WithMany(r => r.Likes)
                .HasForeignKey(l => l.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict on the user side: the Review->User cascade already reaches
            // AspNetUsers, and a second cascade path would trip SQL Server.
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // A user can like a given review only once.
            entity.HasIndex(l => new { l.ReviewId, l.UserId }).IsUnique();
        });

        builder.Entity<ReviewComment>(entity =>
        {
            entity.HasOne(c => c.Review)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.ReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict for the same multiple-cascade-path reason as ReviewLike.
            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FriendRequest>(entity =>
        {
            // Both sides Restrict: two cascade paths to AspNetUsers would trip
            // SQL Server (same reason as ReviewLike/ReviewComment above).
            entity.HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Restrict);

            // At most one request per ordered pair.
            entity.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
        });
    }
}

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
    }
}

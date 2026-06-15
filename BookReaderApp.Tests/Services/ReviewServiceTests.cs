using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class ReviewServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<Book> SeedBookAsync(ApplicationDbContext context)
    {
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        return book;
    }

    [Fact]
    public async Task SaveReviewAsync_WhenNoExistingReview_CreatesReview()
    {
        using var context = NewContext();
        var book = await SeedBookAsync(context);

        var service = new ReviewService(new ReviewRepository(context));
        await service.SaveReviewAsync("user-1", book.Id, "Great read", containsSpoilers: false);

        var review = Assert.Single(context.Reviews);
        Assert.Equal("user-1", review.UserId);
        Assert.Equal(book.Id, review.BookId);
        Assert.Equal("Great read", review.Text);
        Assert.False(review.ContainsSpoilers);
    }

    [Fact]
    public async Task SaveReviewAsync_WhenReviewExists_UpdatesTextAndSpoilerAndUpdatedAt()
    {
        using var context = NewContext();
        var book = await SeedBookAsync(context);
        var created = DateTime.UtcNow.AddDays(-1);
        context.Reviews.Add(new Review
        {
            UserId = "user-1",
            BookId = book.Id,
            Text = "Old",
            ContainsSpoilers = false,
            CreatedAt = created,
            UpdatedAt = created,
        });
        await context.SaveChangesAsync();

        var service = new ReviewService(new ReviewRepository(context));
        await service.SaveReviewAsync("user-1", book.Id, "New text", containsSpoilers: true);

        var review = Assert.Single(context.Reviews);
        Assert.Equal("New text", review.Text);
        Assert.True(review.ContainsSpoilers);
        Assert.Equal(created, review.CreatedAt);
        Assert.True(review.UpdatedAt > created);
    }

    [Fact]
    public async Task SaveReviewAsync_WithSpoilerFlag_PersistsFlag()
    {
        using var context = NewContext();
        var book = await SeedBookAsync(context);

        var service = new ReviewService(new ReviewRepository(context));
        await service.SaveReviewAsync("user-1", book.Id, "The butler did it", containsSpoilers: true);

        var review = Assert.Single(context.Reviews);
        Assert.True(review.ContainsSpoilers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveReviewAsync_WithBlankText_Throws(string text)
    {
        using var context = NewContext();
        var service = new ReviewService(new ReviewRepository(context));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveReviewAsync("user-1", 1, text, containsSpoilers: false));
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenReviewExists_RemovesIt()
    {
        using var context = NewContext();
        var book = await SeedBookAsync(context);
        context.Reviews.Add(new Review { UserId = "user-1", BookId = book.Id, Text = "x" });
        await context.SaveChangesAsync();

        var service = new ReviewService(new ReviewRepository(context));
        await service.DeleteReviewAsync("user-1", book.Id);

        Assert.Empty(context.Reviews);
    }

    [Fact]
    public async Task DeleteReviewAsync_WhenNoReview_DoesNothing()
    {
        using var context = NewContext();
        var service = new ReviewService(new ReviewRepository(context));

        await service.DeleteReviewAsync("user-1", 999);

        Assert.Empty(context.Reviews);
    }

    [Fact]
    public async Task GetForBookAsync_ReturnsReviewsNewestFirst_WithUser()
    {
        using var context = NewContext();
        var book = await SeedBookAsync(context);
        context.Users.AddRange(
            new ApplicationUser { Id = "user-1", UserName = "u1", DisplayName = "First" },
            new ApplicationUser { Id = "user-2", UserName = "u2", DisplayName = "Second" });
        context.Reviews.AddRange(
            new Review { UserId = "user-1", BookId = book.Id, Text = "older", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Review { UserId = "user-2", BookId = book.Id, Text = "newer", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new ReviewService(new ReviewRepository(context));
        var reviews = await service.GetForBookAsync(book.Id);

        Assert.Equal(2, reviews.Count);
        Assert.Equal("newer", reviews[0].Text);
        Assert.Equal("Second", reviews[0].User?.DisplayName);
        Assert.Equal("older", reviews[1].Text);
    }
}

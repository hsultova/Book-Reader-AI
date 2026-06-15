using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class ReviewCommentServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ReviewCommentService NewService(ApplicationDbContext context) =>
        new(new ReviewCommentRepository(context), new ReviewRepository(context));

    // Seeds a book and a review authored by "author-1"; returns the review id.
    private static async Task<int> SeedReviewAsync(ApplicationDbContext context, string authorId = "author-1")
    {
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        var review = new Review { UserId = authorId, BookId = book.Id, Text = "Nice" };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();
        return review.Id;
    }

    [Fact]
    public async Task AddCommentAsync_WithValidText_PersistsComment()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);

        await service.AddCommentAsync("reader-2", reviewId, "  Great point!  ");

        var comment = Assert.Single(context.ReviewComments);
        Assert.Equal(reviewId, comment.ReviewId);
        Assert.Equal("reader-2", comment.UserId);
        Assert.Equal("Great point!", comment.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCommentAsync_WithBlankText_Throws(string text)
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddCommentAsync("reader-2", reviewId, text));
    }

    [Fact]
    public async Task AddCommentAsync_OnOwnReview_DoesNothing()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context, authorId: "author-1");
        var service = NewService(context);

        await service.AddCommentAsync("author-1", reviewId, "commenting on myself");

        Assert.Empty(context.ReviewComments);
    }

    [Fact]
    public async Task AddCommentAsync_OnMissingReview_DoesNothing()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.AddCommentAsync("reader-2", 999, "hello");

        Assert.Empty(context.ReviewComments);
    }

    [Fact]
    public async Task DeleteCommentAsync_ByAuthor_RemovesComment()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);
        await service.AddCommentAsync("reader-2", reviewId, "to be deleted");
        var commentId = (await context.ReviewComments.SingleAsync()).Id;

        await service.DeleteCommentAsync("reader-2", commentId);

        Assert.Empty(context.ReviewComments);
    }

    [Fact]
    public async Task DeleteCommentAsync_ByNonAuthor_DoesNothing()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);
        await service.AddCommentAsync("reader-2", reviewId, "keep me");
        var commentId = (await context.ReviewComments.SingleAsync()).Id;

        await service.DeleteCommentAsync("reader-3", commentId);

        Assert.Single(context.ReviewComments);
    }

    [Fact]
    public async Task GetCommentsForReviewsAsync_ReturnsCommentsOldestFirst_WithUser()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        context.Users.Add(new ApplicationUser { Id = "reader-2", UserName = "r2", DisplayName = "Reader Two" });
        await context.SaveChangesAsync();
        context.ReviewComments.AddRange(
            new ReviewComment { ReviewId = reviewId, UserId = "reader-2", Text = "first", CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new ReviewComment { ReviewId = reviewId, UserId = "reader-2", Text = "second", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        var service = NewService(context);

        var byReview = await service.GetCommentsForReviewsAsync(new[] { reviewId });

        var comments = byReview[reviewId];
        Assert.Equal(2, comments.Count);
        Assert.Equal("first", comments[0].Text);
        Assert.Equal("Reader Two", comments[0].User?.DisplayName);
        Assert.Equal("second", comments[1].Text);
    }
}

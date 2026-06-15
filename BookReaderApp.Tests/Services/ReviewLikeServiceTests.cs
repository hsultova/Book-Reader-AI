using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class ReviewLikeServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ReviewLikeService NewService(ApplicationDbContext context) =>
        new(new ReviewLikeRepository(context), new ReviewRepository(context));

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
    public async Task ToggleLikeAsync_WhenNotLiked_AddsLike()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);

        await service.ToggleLikeAsync("reader-2", reviewId);

        var like = Assert.Single(context.ReviewLikes);
        Assert.Equal(reviewId, like.ReviewId);
        Assert.Equal("reader-2", like.UserId);
    }

    [Fact]
    public async Task ToggleLikeAsync_WhenAlreadyLiked_RemovesLike()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);

        await service.ToggleLikeAsync("reader-2", reviewId);
        await service.ToggleLikeAsync("reader-2", reviewId);

        Assert.Empty(context.ReviewLikes);
    }

    [Fact]
    public async Task ToggleLikeAsync_OnOwnReview_DoesNothing()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context, authorId: "author-1");
        var service = NewService(context);

        await service.ToggleLikeAsync("author-1", reviewId);

        Assert.Empty(context.ReviewLikes);
    }

    [Fact]
    public async Task ToggleLikeAsync_OnMissingReview_DoesNothing()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.ToggleLikeAsync("reader-2", 999);

        Assert.Empty(context.ReviewLikes);
    }

    [Fact]
    public async Task GetLikeCountsAsync_ReturnsCountPerReview()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);
        await service.ToggleLikeAsync("reader-2", reviewId);
        await service.ToggleLikeAsync("reader-3", reviewId);

        var counts = await service.GetLikeCountsAsync(new[] { reviewId });

        Assert.Equal(2, counts[reviewId]);
    }

    [Fact]
    public async Task GetLikedReviewIdsAsync_ReturnsOnlyUsersLikedReviews()
    {
        using var context = NewContext();
        var reviewId = await SeedReviewAsync(context);
        var service = NewService(context);
        await service.ToggleLikeAsync("reader-2", reviewId);

        var likedByReader = await service.GetLikedReviewIdsAsync("reader-2", new[] { reviewId });
        var likedByOther = await service.GetLikedReviewIdsAsync("reader-3", new[] { reviewId });

        Assert.Contains(reviewId, likedByReader);
        Assert.DoesNotContain(reviewId, likedByOther);
    }
}

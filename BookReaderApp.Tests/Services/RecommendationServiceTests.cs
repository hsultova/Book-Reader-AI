using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class RecommendationServiceTests
{
    private const string UserId = "user-1";

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static RecommendationService NewService(ApplicationDbContext context) =>
        new(new UserBookRepository(context), new BookRepository(context));

    // Rates a book for UserId so it becomes a recommendation source.
    private static void Rate(ApplicationDbContext context, Book book, int rating) =>
        context.UserBooks.Add(new UserBook
        {
            UserId = UserId,
            Book = book,
            Rating = rating,
            RatedAt = DateTime.UtcNow
        });

    [Fact]
    public async Task GetRecommendationsAsync_WithNoHighRatedBooks_ReturnsEmpty()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        var rated = new Book { Title = "Liked OK", AuthorId = author.Id, Isbn = "1" };
        var other = new Book { Title = "Same Author", AuthorId = author.Id, Isbn = "2" };
        context.Books.AddRange(rated, other);
        Rate(context, rated, rating: 3); // below the 4-star threshold
        await context.SaveChangesAsync();

        var result = await NewService(context).GetRecommendationsAsync(UserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithHighRatedBook_ReturnsGroupTitledAfterIt()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        var genre = new Genre { Name = "Sci-Fi" };
        context.Authors.Add(author);
        context.Genres.Add(genre);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, GenreId = genre.Id, Isbn = "1" };
        var sameAuthor = new Book { Title = "Dune Messiah", AuthorId = author.Id, Isbn = "2" };
        var sameGenre = new Book { Title = "Neuromancer", AuthorId = author.Id, GenreId = genre.Id, Isbn = "3" };
        context.Books.AddRange(dune, sameAuthor, sameGenre);
        Rate(context, dune, rating: 5);
        await context.SaveChangesAsync();

        var result = await NewService(context).GetRecommendationsAsync(UserId);

        var group = Assert.Single(result);
        Assert.Equal("Because you enjoyed \"Dune\"", group.Title);
        Assert.Contains(group.Books, b => b.Title == "Dune Messiah");
        Assert.Contains(group.Books, b => b.Title == "Neuromancer");
        Assert.DoesNotContain(group.Books, b => b.Title == "Dune"); // source excluded
    }

    [Fact]
    public async Task GetRecommendationsAsync_ExcludesBooksAlreadyOnUserShelves()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        context.Authors.Add(author);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, Isbn = "1" };
        var owned = new Book { Title = "Dune Messiah", AuthorId = author.Id, Isbn = "2" };
        var fresh = new Book { Title = "Children of Dune", AuthorId = author.Id, Isbn = "3" };
        context.Books.AddRange(dune, owned, fresh);
        Rate(context, dune, rating: 5);
        // Already on a shelf (no rating) — should not be recommended back.
        context.UserBooks.Add(new UserBook { UserId = UserId, Book = owned, Status = ReadingStatus.WantToRead });
        await context.SaveChangesAsync();

        var result = await NewService(context).GetRecommendationsAsync(UserId);

        var group = Assert.Single(result);
        Assert.Contains(group.Books, b => b.Title == "Children of Dune");
        Assert.DoesNotContain(group.Books, b => b.Title == "Dune Messiah");
    }

    [Fact]
    public async Task GetRecommendationsAsync_DoesNotRepeatSameBookAcrossGroups()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        var genre = new Genre { Name = "Sci-Fi" };
        context.Authors.Add(author);
        context.Genres.Add(genre);
        // Two highly-rated sources that share the same genre.
        var dune = new Book { Title = "Dune", AuthorId = author.Id, GenreId = genre.Id, Isbn = "1" };
        var foundation = new Book { Title = "Foundation", AuthorId = author.Id, GenreId = genre.Id, Isbn = "2" };
        // A single candidate that matches both sources' genre.
        var candidate = new Book { Title = "Hyperion", AuthorId = author.Id, GenreId = genre.Id, Isbn = "3" };
        context.Books.AddRange(dune, foundation, candidate);
        Rate(context, dune, rating: 5);
        Rate(context, foundation, rating: 4);
        await context.SaveChangesAsync();

        var result = await NewService(context).GetRecommendationsAsync(UserId);

        var appearances = result.SelectMany(g => g.Books).Count(b => b.Title == "Hyperion");
        Assert.Equal(1, appearances);
    }
}

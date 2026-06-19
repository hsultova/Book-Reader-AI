using BookReaderApp.Configuration;
using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookReaderApp.Tests.Services;

public class AiRecommendationServiceTests
{
    private const string UserId = "user-1";

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AiRecommendationService NewService(
        ApplicationDbContext context, IBookRecommendationEngine engine, int max = 6) =>
        new(new UserBookRepository(context),
            new BookRepository(context),
            engine,
            Options.Create(new GeminiOptions { MaxRecommendations = max }));

    private static void Rate(ApplicationDbContext context, Book book, int rating) =>
        context.UserBooks.Add(new UserBook
        {
            UserId = UserId,
            Book = book,
            Rating = rating,
            RatedAt = DateTime.UtcNow
        });

    // A fake engine that records the candidates it was given and returns scripted picks.
    private sealed class FakeEngine : IBookRecommendationEngine
    {
        private readonly Func<IReadOnlyList<Book>, IReadOnlyList<BookPick>> _pick;
        public IReadOnlyList<Book> ReceivedCandidates { get; private set; } = Array.Empty<Book>();
        public bool WasCalled { get; private set; }

        public FakeEngine(Func<IReadOnlyList<Book>, IReadOnlyList<BookPick>> pick) => _pick = pick;

        public Task<IReadOnlyList<BookPick>> SuggestAsync(
            IReadOnlyList<Book> likedBooks, IReadOnlyList<Book> candidates, int max, CancellationToken ct = default)
        {
            WasCalled = true;
            ReceivedCandidates = candidates;
            return Task.FromResult(_pick(candidates));
        }
    }

    [Fact]
    public async Task GetAiRecommendationsAsync_WithNoHighRatedBooks_ReturnsEmptyAndDoesNotCallEngine()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        var liked = new Book { Title = "Liked OK", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(liked);
        Rate(context, liked, rating: 3); // below the 4-star threshold
        await context.SaveChangesAsync();

        var engine = new FakeEngine(_ => Array.Empty<BookPick>());

        var result = await NewService(context, engine).GetAiRecommendationsAsync(UserId);

        Assert.Empty(result);
        Assert.False(engine.WasCalled);
    }

    [Fact]
    public async Task GetAiRecommendationsAsync_MapsPicksToBooksWithReasons()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        var genre = new Genre { Name = "Sci-Fi" };
        context.Authors.Add(author);
        context.Genres.Add(genre);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, GenreId = genre.Id, Isbn = "1" };
        var messiah = new Book { Title = "Dune Messiah", AuthorId = author.Id, GenreId = genre.Id, Isbn = "2" };
        context.Books.AddRange(dune, messiah);
        Rate(context, dune, rating: 5);
        await context.SaveChangesAsync();

        // Pick the first candidate (Dune Messiah) with a reason.
        var engine = new FakeEngine(c => new[] { new BookPick(c[0].Id, "Continues Dune's saga.") });

        var result = await NewService(context, engine).GetAiRecommendationsAsync(UserId);

        var rec = Assert.Single(result);
        Assert.Equal("Dune Messiah", rec.Book.Title);
        Assert.Equal("Continues Dune's saga.", rec.Reason);
    }

    [Fact]
    public async Task GetAiRecommendationsAsync_DropsIdsNotInCandidatePool()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        context.Authors.Add(author);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, Isbn = "1" };
        var messiah = new Book { Title = "Dune Messiah", AuthorId = author.Id, Isbn = "2" };
        context.Books.AddRange(dune, messiah);
        Rate(context, dune, rating: 5);
        await context.SaveChangesAsync();

        // Return a valid candidate plus a hallucinated id that was never offered.
        var engine = new FakeEngine(c => new[]
        {
            new BookPick(c[0].Id, "Real pick."),
            new BookPick(999999, "Invented book.")
        });

        var result = await NewService(context, engine).GetAiRecommendationsAsync(UserId);

        var rec = Assert.Single(result);
        Assert.Equal("Dune Messiah", rec.Book.Title);
    }

    [Fact]
    public async Task GetAiRecommendationsAsync_ExcludesOwnedBooksFromCandidatePool()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        context.Authors.Add(author);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, Isbn = "1" };
        var owned = new Book { Title = "Dune Messiah", AuthorId = author.Id, Isbn = "2" };
        var fresh = new Book { Title = "Children of Dune", AuthorId = author.Id, Isbn = "3" };
        context.Books.AddRange(dune, owned, fresh);
        Rate(context, dune, rating: 5);
        context.UserBooks.Add(new UserBook { UserId = UserId, Book = owned, Status = ReadingStatus.WantToRead });
        await context.SaveChangesAsync();

        var engine = new FakeEngine(_ => Array.Empty<BookPick>());

        await NewService(context, engine).GetAiRecommendationsAsync(UserId);

        Assert.Contains(engine.ReceivedCandidates, b => b.Title == "Children of Dune");
        Assert.DoesNotContain(engine.ReceivedCandidates, b => b.Title == "Dune Messiah"); // owned
        Assert.DoesNotContain(engine.ReceivedCandidates, b => b.Title == "Dune");          // the source
    }

    [Fact]
    public async Task GetAiRecommendationsAsync_CapsResultsAtMaxRecommendations()
    {
        using var context = NewContext();
        var author = new Author { Name = "Frank Herbert" };
        context.Authors.Add(author);
        var dune = new Book { Title = "Dune", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(dune);
        Rate(context, dune, rating: 5);
        for (var i = 0; i < 5; i++)
        {
            context.Books.Add(new Book { Title = $"Sequel {i}", AuthorId = author.Id, Isbn = $"s{i}" });
        }
        await context.SaveChangesAsync();

        // The engine returns every candidate; the service should cap at max = 2.
        var engine = new FakeEngine(c => c.Select(b => new BookPick(b.Id, "Reason")).ToList());

        var result = await NewService(context, engine, max: 2).GetAiRecommendationsAsync(UserId);

        Assert.Equal(2, result.Count);
    }
}

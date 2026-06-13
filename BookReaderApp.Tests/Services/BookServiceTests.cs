using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookReaderApp.Tests.Services;

public class BookServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static BookService NewService(ApplicationDbContext context) =>
        new(new EfRepository<Book>(context), new AuthorRepository(context), new GenreRepository(context), NullLogger<BookService>.Instance);

    private static async Task<int> SeedAuthorAsync(ApplicationDbContext context)
    {
        var author = new Author { Name = "Robert C. Martin" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        return author.Id;
    }

    private static BookFormViewModel SampleForm(int authorId) => new()
    {
        Title = "Clean Code",
        AuthorValue = authorId.ToString(),
        Isbn = "978-0132350884",
        GenreValue = "Software",
        Description = "A handbook of agile software craftsmanship.",
        CoverImageUrl = "https://example.com/clean-code.jpg"
    };

    [Fact]
    public async Task CreateBookAsync_WithValidInput_PersistsBook()
    {
        using var context = NewContext();
        var service = NewService(context);
        var authorId = await SeedAuthorAsync(context);

        var result = await service.CreateBookAsync(SampleForm(authorId));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.BookId);
        var stored = await context.Books.SingleAsync();
        Assert.Equal("Clean Code", stored.Title);
        Assert.Equal(authorId, stored.AuthorId); // existing author resolved by ID string
    }

    [Fact]
    public async Task GetBookByIdAsync_WithMissingId_ReturnsNull()
    {
        using var context = NewContext();
        var service = NewService(context);

        var book = await service.GetBookByIdAsync(999);

        Assert.Null(book);
    }

    [Fact]
    public async Task UpdateBookAsync_WithValidInput_UpdatesFields()
    {
        using var context = NewContext();
        var service = NewService(context);
        var authorId = await SeedAuthorAsync(context);
        var created = await service.CreateBookAsync(SampleForm(authorId));

        var edit = SampleForm(authorId);
        edit.Title = "Clean Code (Revised)";
        edit.GenreValue = "Programming";
        edit.Genre = new Genre { Name = "Software" };
        var result = await service.UpdateBookAsync(created.BookId!.Value, edit);

        Assert.True(result.Succeeded);
        var stored = await context.Books.Include(b => b.Genre).SingleAsync();
        Assert.Equal("Clean Code (Revised)", stored.Title);
        Assert.Equal("Software", stored.Genre!.Name);
        Assert.Equal("Programming", stored.Genre!.Name);
    }

    [Fact]
    public async Task UpdateBookAsync_WithMissingId_ReturnsNotFound()
    {
        using var context = NewContext();
        var service = NewService(context);
        var authorId = await SeedAuthorAsync(context);

        var result = await service.UpdateBookAsync(999, SampleForm(authorId));

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DeleteBookAsync_WithExistingId_RemovesBook()
    {
        using var context = NewContext();
        var service = NewService(context);
        var authorId = await SeedAuthorAsync(context);
        var created = await service.CreateBookAsync(SampleForm(authorId));

        var deleted = await service.DeleteBookAsync(created.BookId!.Value);

        Assert.True(deleted);
        Assert.False(await context.Books.AnyAsync());
    }

    [Fact]
    public async Task DeleteBookAsync_WithMissingId_ReturnsFalse()
    {
        using var context = NewContext();
        var service = NewService(context);

        var deleted = await service.DeleteBookAsync(999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetBooksAsync_PagesResults()
    {
        using var context = NewContext();
        var service = NewService(context);
        var authorId = await SeedAuthorAsync(context);
        for (var i = 0; i < 25; i++)
        {
            var form = SampleForm(authorId);
            form.Title = $"Book {i}";
            await service.CreateBookAsync(form);
        }

        var page1 = await service.GetBooksAsync(page: 1, pageSize: 20);
        var page2 = await service.GetBooksAsync(page: 2, pageSize: 20);

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal(2, page1.TotalPages);
    }
}

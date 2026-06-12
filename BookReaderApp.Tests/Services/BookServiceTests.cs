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
        new(new EfRepository<Book>(context), NullLogger<BookService>.Instance);

    private static BookFormViewModel SampleForm() => new()
    {
        Title = "Clean Code",
        Author = "Robert C. Martin",
        Isbn = "978-0132350884",
        Genre = "Software",
        Description = "A handbook of agile software craftsmanship.",
        CoverImageUrl = "https://example.com/clean-code.jpg"
    };

    [Fact]
    public async Task CreateBookAsync_WithValidInput_PersistsBook()
    {
        using var context = NewContext();
        var service = NewService(context);

        var result = await service.CreateBookAsync(SampleForm());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.BookId);
        var stored = await context.Books.SingleAsync();
        Assert.Equal("Clean Code", stored.Title);
        Assert.Equal("Robert C. Martin", stored.Author);
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
        var created = await service.CreateBookAsync(SampleForm());

        var edit = SampleForm();
        edit.Title = "Clean Code (Revised)";
        edit.Genre = "Programming";
        var result = await service.UpdateBookAsync(created.BookId!.Value, edit);

        Assert.True(result.Succeeded);
        var stored = await context.Books.SingleAsync();
        Assert.Equal("Clean Code (Revised)", stored.Title);
        Assert.Equal("Programming", stored.Genre);
    }

    [Fact]
    public async Task UpdateBookAsync_WithMissingId_ReturnsNotFound()
    {
        using var context = NewContext();
        var service = NewService(context);

        var result = await service.UpdateBookAsync(999, SampleForm());

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DeleteBookAsync_WithExistingId_RemovesBook()
    {
        using var context = NewContext();
        var service = NewService(context);
        var created = await service.CreateBookAsync(SampleForm());

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
        for (var i = 0; i < 25; i++)
        {
            var form = SampleForm();
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

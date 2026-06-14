using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class UserBookServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetMyBooksAsync_ReturnsOnlyUsersBooks_WithBookIncluded()
    {
        using var context = NewContext();
        var authorA = new Author { Name = "A" };
        var authorB = new Author { Name = "B" };
        context.Authors.AddRange(authorA, authorB);
        await context.SaveChangesAsync();
        var mine = new Book { Title = "Mine", AuthorId = authorA.Id, Isbn = "1" };
        var other = new Book { Title = "Other", AuthorId = authorB.Id, Isbn = "2" };
        context.Books.AddRange(mine, other);
        context.UserBooks.AddRange(
            new UserBook { UserId = "user-1", Book = mine },
            new UserBook { UserId = "user-2", Book = other });
        await context.SaveChangesAsync();

        var service = new UserBookService(new UserBookRepository(context));
        var result = await service.GetMyBooksAsync("user-1");

        var entry = Assert.Single(result);
        Assert.Equal("user-1", entry.UserId);
        Assert.NotNull(entry.Book);
        Assert.Equal("Mine", entry.Book!.Title);
    }

    [Fact]
    public async Task GetMyBooksAsync_WithNoBooks_ReturnsEmpty()
    {
        using var context = NewContext();
        var service = new UserBookService(new UserBookRepository(context));

        var result = await service.GetMyBooksAsync("nobody");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SetStatusAsync_WhenBookNotOnShelf_AddsUserBookWithStatus()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var service = new UserBookService(new UserBookRepository(context));
        await service.SetStatusAsync("user-1", book.Id, ReadingStatus.Reading);

        var entry = Assert.Single(context.UserBooks);
        Assert.Equal("user-1", entry.UserId);
        Assert.Equal(book.Id, entry.BookId);
        Assert.Equal(ReadingStatus.Reading, entry.Status);
    }

    [Fact]
    public async Task SetStatusAsync_WhenBookAlreadyOnShelf_UpdatesStatus()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        context.UserBooks.Add(new UserBook { UserId = "user-1", Book = book, Status = ReadingStatus.WantToRead });
        await context.SaveChangesAsync();

        var service = new UserBookService(new UserBookRepository(context));
        await service.SetStatusAsync("user-1", book.Id, ReadingStatus.Finished);

        var entry = Assert.Single(context.UserBooks);
        Assert.Equal(ReadingStatus.Finished, entry.Status);
    }

    [Fact]
    public async Task RemoveAsync_WhenBookOnShelf_RemovesEntry()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        context.UserBooks.Add(new UserBook { UserId = "user-1", Book = book });
        await context.SaveChangesAsync();

        var service = new UserBookService(new UserBookRepository(context));
        await service.RemoveAsync("user-1", book.Id);

        Assert.Empty(context.UserBooks);
    }

    [Fact]
    public async Task RemoveAsync_WhenBookNotOnShelf_DoesNothing()
    {
        using var context = NewContext();
        var service = new UserBookService(new UserBookRepository(context));

        await service.RemoveAsync("user-1", 999);

        Assert.Empty(context.UserBooks);
    }
}

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
}

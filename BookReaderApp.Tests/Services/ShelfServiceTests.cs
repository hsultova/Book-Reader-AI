using BookReaderApp.Data;
using BookReaderApp.Models;
using BookReaderApp.Repositories;
using BookReaderApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookReaderApp.Tests.Services;

public class ShelfServiceTests
{
    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetOrCreateAsync_WhenNameNew_CreatesShelf()
    {
        using var context = NewContext();
        var service = new ShelfService(new ShelfRepository(context), new UserBookRepository(context));

        var shelf = await service.GetOrCreateAsync("user-1", "  Favorites  ");

        Assert.Equal("Favorites", shelf.Name);
        Assert.Equal("user-1", shelf.UserId);
        Assert.Equal(shelf.Id, Assert.Single(context.Shelves).Id);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNameExists_ReturnsExisting()
    {
        using var context = NewContext();
        context.Shelves.Add(new Shelf { UserId = "user-1", Name = "Favorites" });
        await context.SaveChangesAsync();

        var service = new ShelfService(new ShelfRepository(context), new UserBookRepository(context));
        var shelf = await service.GetOrCreateAsync("user-1", "favorites");

        Assert.Single(context.Shelves);
        Assert.Equal("Favorites", shelf.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesShelf_AndMovesBooksToWantToRead()
    {
        using var context = NewContext();
        var author = new Author { Name = "A" };
        context.Authors.Add(author);
        await context.SaveChangesAsync();
        var book = new Book { Title = "Book", AuthorId = author.Id, Isbn = "1" };
        context.Books.Add(book);
        var shelf = new Shelf { UserId = "user-1", Name = "Favorites" };
        context.Shelves.Add(shelf);
        await context.SaveChangesAsync();
        context.UserBooks.Add(new UserBook { UserId = "user-1", BookId = book.Id, ShelfId = shelf.Id });
        await context.SaveChangesAsync();

        var service = new ShelfService(new ShelfRepository(context), new UserBookRepository(context));
        await service.DeleteAsync("user-1", shelf.Id);

        Assert.Empty(context.Shelves);
        var entry = Assert.Single(context.UserBooks);
        Assert.Null(entry.ShelfId);
        Assert.Equal(ReadingStatus.WantToRead, entry.Status);
    }

    [Fact]
    public async Task DeleteAsync_WhenShelfNotOwnedByUser_DoesNothing()
    {
        using var context = NewContext();
        var shelf = new Shelf { UserId = "owner", Name = "Favorites" };
        context.Shelves.Add(shelf);
        await context.SaveChangesAsync();

        var service = new ShelfService(new ShelfRepository(context), new UserBookRepository(context));
        await service.DeleteAsync("intruder", shelf.Id);

        Assert.Single(context.Shelves);
    }
}

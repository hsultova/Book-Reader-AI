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
        var service = new ShelfService(new ShelfRepository(context));

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

        var service = new ShelfService(new ShelfRepository(context));
        var shelf = await service.GetOrCreateAsync("user-1", "favorites");

        Assert.Single(context.Shelves);
        Assert.Equal("Favorites", shelf.Name);
    }
}

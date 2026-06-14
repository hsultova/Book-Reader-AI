using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

// The signed-in user's personal bookshelf.
[Authorize]
public class MyBooksController : Controller
{
    private readonly IUserBookService _userBookService;
    private readonly IShelfService _shelfService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyBooksController(
        IUserBookService userBookService,
        IShelfService shelfService,
        UserManager<ApplicationUser> userManager)
    {
        _userBookService = userBookService;
        _shelfService = shelfService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ReadingStatus? status, int? shelfId)
    {
        var userId = _userManager.GetUserId(User)!;
        var all = await _userBookService.GetMyBooksAsync(userId);
        var shelves = await _shelfService.GetShelvesAsync(userId);

        var counts = Enum.GetValues<ReadingStatus>()
            .ToDictionary(s => s, s => all.Count(ub => ub.Status == s));
        var shelfCounts = shelves.ToDictionary(s => s.Id, s => all.Count(ub => ub.ShelfId == s.Id));

        IReadOnlyList<UserBook> books = all;
        if (status is not null)
        {
            books = all.Where(ub => ub.Status == status).ToList();
        }
        else if (shelfId is not null)
        {
            books = all.Where(ub => ub.ShelfId == shelfId).ToList();
        }

        return View(new MyBooksViewModel
        {
            Books = books,
            SelectedStatus = status,
            SelectedShelfId = shelfId,
            TotalCount = all.Count,
            Counts = counts,
            CustomShelves = shelves,
            ShelfCounts = shelfCounts,
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int bookId, ReadingStatus status, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        await _userBookService.SetStatusAsync(userId, bookId, status);
        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> SetShelf(int bookId, int shelfId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;

        // Only place the book if the shelf actually belongs to this user.
        var shelves = await _shelfService.GetShelvesAsync(userId);
        if (shelves.Any(s => s.Id == shelfId))
        {
            await _userBookService.SetShelfAsync(userId, bookId, shelfId);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShelf(string name, int bookId, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var userId = _userManager.GetUserId(User)!;
            var shelf = await _shelfService.GetOrCreateAsync(userId, name);
            await _userBookService.SetShelfAsync(userId, bookId, shelf.Id);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteShelf(int shelfId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _shelfService.DeleteAsync(userId, shelfId);
        // Always land on the full list — the filtered shelf no longer exists.
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int bookId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        await _userBookService.RemoveAsync(userId, bookId);
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
}

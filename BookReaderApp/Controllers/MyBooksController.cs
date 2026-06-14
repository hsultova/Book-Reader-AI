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
    private readonly UserManager<ApplicationUser> _userManager;

    public MyBooksController(
        IUserBookService userBookService,
        UserManager<ApplicationUser> userManager)
    {
        _userBookService = userBookService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ReadingStatus? status)
    {
        var userId = _userManager.GetUserId(User)!;
        var all = await _userBookService.GetMyBooksAsync(userId);

        var counts = Enum.GetValues<ReadingStatus>()
            .ToDictionary(s => s, s => all.Count(ub => ub.Status == s));

        var books = status is null
            ? all
            : all.Where(ub => ub.Status == status).ToList();

        return View(new MyBooksViewModel
        {
            Books = books,
            SelectedStatus = status,
            TotalCount = all.Count,
            Counts = counts,
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

using BookReaderApp.Models;
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
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var books = await _userBookService.GetMyBooksAsync(userId);
        return View(books);
    }
}

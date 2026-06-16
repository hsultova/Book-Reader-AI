using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;

namespace BookReaderApp.Controllers;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUpdatesService _updates;

    public HomeController(UserManager<ApplicationUser> userManager, IUpdatesService updates)
    {
        _userManager = userManager;
        _updates = updates;
    }

    public async Task<IActionResult> Index()
    {
        // Anonymous visitors get the sign-in prompt (null model); signed-in users get the feed.
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(model: null);
        }

        var userId = _userManager.GetUserId(User)!;
        var model = new HomeViewModel { Updates = await _updates.GetFeedAsync(userId) };
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

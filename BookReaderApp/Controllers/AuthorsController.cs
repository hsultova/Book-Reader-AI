using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

// Author browse and follow. Browsing is open to everyone; follow/unfollow require login.
public class AuthorsController : Controller
{
    private readonly IAuthorService _authorService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthorsController(
        IAuthorService authorService,
        UserManager<ApplicationUser> userManager)
    {
        _authorService = authorService;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var author = await _authorService.GetAuthorWithBooksAsync(id);
        if (author is null)
        {
            return NotFound();
        }

        var followerCount = await _authorService.GetFollowerCountAsync(id);
        var isFollowing = false;
        var isAuthenticated = User.Identity?.IsAuthenticated == true;

        if (isAuthenticated)
        {
            var userId = _userManager.GetUserId(User)!;
            isFollowing = await _authorService.IsFollowingAsync(userId, id);
        }

        var vm = new AuthorDetailViewModel
        {
            Author = author,
            FollowerCount = followerCount,
            IsFollowing = isFollowing,
            IsAuthenticated = isAuthenticated
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Follow(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _authorService.FollowAsync(userId, id);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Unfollow(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _authorService.UnfollowAsync(userId, id);
        return RedirectToAction(nameof(Details), new { id });
    }
}

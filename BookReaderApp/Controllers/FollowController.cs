using BookReaderApp.Models;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

[Authorize]
public class FollowController : Controller
{
    private readonly IFollowService _follows;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowController(
        IFollowService follows,
        UserManager<ApplicationUser> userManager)
    {
        _follows = follows;
        _userManager = userManager;
    }

    // Follow user `id`, then return to their profile.
    [HttpPost]
    public async Task<IActionResult> Follow(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User)!;
        await _follows.FollowAsync(userId, id);
        return RedirectToAction("Index", "Profile", new { id });
    }

    // Unfollow user `id`. Returns to returnUrl when it's a safe local path (e.g. the
    // Friends page), otherwise back to the user's profile.
    [HttpPost]
    public async Task<IActionResult> Unfollow(string id, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User)!;
        await _follows.UnfollowAsync(userId, id);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Profile", new { id });
    }
}

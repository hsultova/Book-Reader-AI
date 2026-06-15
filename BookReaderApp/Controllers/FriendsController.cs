using BookReaderApp.Models;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly IFriendRequestService _friendRequests;
    private readonly UserManager<ApplicationUser> _userManager;

    public FriendsController(
        IFriendRequestService friendRequests,
        UserManager<ApplicationUser> userManager)
    {
        _friendRequests = friendRequests;
        _userManager = userManager;
    }

    // The current user's friends and pending requests (both directions).
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var model = await _friendRequests.GetFriendsPageAsync(userId);
        return View(model);
    }

    // Send a friend request to user `id`, then return to their profile.
    [HttpPost]
    public async Task<IActionResult> Send(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest();
        }

        var userId = _userManager.GetUserId(User)!;
        await _friendRequests.SendRequestAsync(userId, id);
        return RedirectToAction("Index", "Profile", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _friendRequests.AcceptAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _friendRequests.RejectAsync(id, userId);
        return RedirectToAction(nameof(Index));
    }
}

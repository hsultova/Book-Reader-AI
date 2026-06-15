using BookReaderApp.Models;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

// Write, edit, and delete the signed-in user's text reviews. Thin: delegates to IReviewService.
[Authorize]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IReviewLikeService _reviewLikeService;
    private readonly IReviewCommentService _reviewCommentService;
    private readonly IUserBookService _userBookService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewsController(
        IReviewService reviewService,
        IReviewLikeService reviewLikeService,
        IReviewCommentService reviewCommentService,
        IUserBookService userBookService,
        UserManager<ApplicationUser> userManager)
    {
        _reviewService = reviewService;
        _reviewLikeService = reviewLikeService;
        _reviewCommentService = reviewCommentService;
        _userBookService = userBookService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Save(int bookId, string text, bool containsSpoilers, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;

        // Only users who have the book on a shelf may review it.
        var shelved = await _userBookService.GetMyBooksAsync(userId);
        if (shelved.Any(ub => ub.BookId == bookId) && !string.IsNullOrWhiteSpace(text))
        {
            await _reviewService.SaveReviewAsync(userId, bookId, text, containsSpoilers);
        }

        return RedirectBack(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int bookId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        await _reviewService.DeleteReviewAsync(userId, bookId);
        return RedirectBack(returnUrl);
    }

    // Like or unlike another reader's review. Works for any book (no shelf requirement);
    // [Authorize] enforces the "logged-in only" rule. Self-likes are ignored by the service.
    [HttpPost]
    public async Task<IActionResult> ToggleLike(int reviewId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        await _reviewLikeService.ToggleLikeAsync(userId, reviewId);
        return RedirectBack(returnUrl);
    }

    // Comment on another reader's review. Self-comments and blank text are ignored.
    [HttpPost]
    public async Task<IActionResult> AddComment(int reviewId, string text, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            var userId = _userManager.GetUserId(User)!;
            await _reviewCommentService.AddCommentAsync(userId, reviewId, text);
        }

        return RedirectBack(returnUrl);
    }

    // Delete one of the current user's own comments.
    [HttpPost]
    public async Task<IActionResult> DeleteComment(int commentId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User)!;
        await _reviewCommentService.DeleteCommentAsync(userId, commentId);
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Books");
}

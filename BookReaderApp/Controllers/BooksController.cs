using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

// Catalog CRUD. Browsing (Index/Details) is open to everyone; mutating actions are
// restricted to Admin/Moderator. Thin: all work delegates to IBookService.
public class BooksController : Controller
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1)
    {
        var books = await _bookService.GetBooksAsync(page);
        return View(books);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public IActionResult Create() => View(new BookFormViewModel());

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Create(BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _bookService.CreateBookAsync(model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id = result.BookId });
        }

        AddErrors(result.Errors);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        ViewData["BookId"] = id;
        return View(ToViewModel(book));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Edit(int id, BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["BookId"] = id;
            return View(model);
        }

        var result = await _bookService.UpdateBookAsync(id, model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        AddErrors(result.Errors);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        if (book is null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _bookService.DeleteBookAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private static BookFormViewModel ToViewModel(Book book) => new()
    {
        Title = book.Title,
        Author = book.Author,
        Isbn = book.Isbn,
        CoverImageUrl = book.CoverImageUrl,
        Description = book.Description,
        Genre = book.Genre
    };
}

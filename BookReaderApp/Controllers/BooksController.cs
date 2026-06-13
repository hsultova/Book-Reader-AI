using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookReaderApp.Controllers;

// Catalog CRUD. Browsing (Index/Details) is open to everyone; mutating actions are
// restricted to Admin/Moderator. Thin: all work delegates to IBookService.
public class BooksController : Controller
{
    private readonly IBookService _bookService;
    private readonly IAuthorService _authorService;

    public BooksController(IBookService bookService, IAuthorService authorService)
    {
        _bookService = bookService;
        _authorService = authorService;
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
    public async Task<IActionResult> Create()
    {
        await PopulateAuthorsAsync();
        return View(new BookFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Create(BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAuthorsAsync();
            return View(model);
        }

        var result = await _bookService.CreateBookAsync(model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id = result.BookId });
        }

        AddErrors(result.Errors);
        await PopulateAuthorsAsync();
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
        await PopulateAuthorsAsync();
        return View(ToViewModel(book));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Edit(int id, BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["BookId"] = id;
            await PopulateAuthorsAsync();
            return View(model);
        }

        var result = await _bookService.UpdateBookAsync(id, model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        AddErrors(result.Errors);
        await PopulateAuthorsAsync();
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

    private async Task PopulateAuthorsAsync()
    {
        var authors = await _authorService.GetAllAuthorsAsync();
        ViewBag.Authors = new SelectList(authors, "Id", "Name");
    }

    private static BookFormViewModel ToViewModel(Book book) => new()
    {
        Title = book.Title,
        AuthorValue = book.AuthorId.ToString(),
        Isbn = book.Isbn,
        CoverImageUrl = book.CoverImageUrl,
        Description = book.Description,
        Genre = book.Genre
    };
}

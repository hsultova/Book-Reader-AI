using BookReaderApp.Models;
using BookReaderApp.Models.ViewModels;
using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookReaderApp.Controllers;

// Catalog CRUD. Browsing (Index/Details) is open to everyone; mutating actions are
// restricted to Admin/Moderator. Thin: all work delegates to IBookService.
public class BooksController : Controller
{
    private readonly IBookService _bookService;
    private readonly IAuthorService _authorService;
    private readonly IGenreService _genreService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IUserBookService _userBookService;
    private readonly UserManager<ApplicationUser> _userManager;

    public BooksController(
        IBookService bookService,
        IAuthorService authorService,
        IGenreService genreService,
        IGoogleBooksService googleBooksService,
        IUserBookService userBookService,
        UserManager<ApplicationUser> userManager)
    {
        _bookService = bookService;
        _authorService = authorService;
        _genreService = genreService;
        _googleBooksService = googleBooksService;
        _userBookService = userBookService;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1)
    {
        var books = await _bookService.GetBooksAsync(page);
        await PopulateShelfStatusesAsync();
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

        await PopulateShelfStatusesAsync();
        return View(book);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new BookFormViewModel());
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Create(BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync();
            return View(model);
        }

        var result = await _bookService.CreateBookAsync(model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id = result.BookId });
        }

        AddErrors(result.Errors);
        await PopulateLookupsAsync();
        return View(model);
    }

    // Backend proxy for the Create page's "fetch from Google Books" search. The browser hits
    // this endpoint (not Google) so the API key stays server-side. Returns JSON for the UI.
    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> SearchGoogleBooks(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(Array.Empty<GoogleBookResult>());
        }

        var results = await _googleBooksService.SearchAsync(query);
        return Json(results);
    }

    // Bulk-create the books the user checked among the Google Books results. Posted as JSON
    // from the Create page; books without an ISBN are skipped and reported back.
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBatch([FromBody] List<BookFormViewModel> books)
    {
        if (books is null || books.Count == 0)
        {
            return Json(new { created = 0, skipped = Array.Empty<string>() });
        }

        var result = await _bookService.CreateBooksAsync(books);
        return Json(new { created = result.CreatedCount, skipped = result.SkippedTitles });
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
        await PopulateLookupsAsync();
        return View(ToViewModel(book));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Moderator}")]
    public async Task<IActionResult> Edit(int id, BookFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["BookId"] = id;
            await PopulateLookupsAsync();
            return View(model);
        }

        var result = await _bookService.UpdateBookAsync(id, model);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        AddErrors(result.Errors);
        await PopulateLookupsAsync();
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

    // Exposes the current user's reading status per book so catalog/detail views can render
    // the shelf dropdown with the right label. Empty for anonymous users.
    private async Task PopulateShelfStatusesAsync()
    {
        var statuses = new Dictionary<int, ReadingStatus>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User)!;
            var shelf = await _userBookService.GetMyBooksAsync(userId);
            foreach (var entry in shelf)
            {
                statuses[entry.BookId] = entry.Status;
            }
        }

        ViewData["ShelfStatuses"] = statuses;
    }

    private void AddErrors(IReadOnlyList<string> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private async Task PopulateLookupsAsync()
    {
        var authors = await _authorService.GetAllAuthorsAsync();
        ViewBag.Authors = new SelectList(authors, "Id", "Name");

        var genres = await _genreService.GetAllGenresAsync();
        ViewBag.Genres = new SelectList(genres, "Id", "Name");
    }

    private static BookFormViewModel ToViewModel(Book book) => new()
    {
        Title = book.Title,
        AuthorValue = book.AuthorId.ToString(),
        Isbn = book.Isbn,
        CoverImageUrl = book.CoverImageUrl,
        Description = book.Description,
        GenreValue = book.GenreId?.ToString()
    };
}

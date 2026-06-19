using BookReaderApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookReaderApp.Controllers;

// Genre browse. Open to everyone: clicking a genre tag lists every book in that genre.
public class GenresController : Controller
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var genre = await _genreService.GetGenreWithBooksAsync(id);
        if (genre is null)
        {
            return NotFound();
        }

        return View(genre);
    }
}

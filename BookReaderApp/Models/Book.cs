using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models;

// Catalog entity: a book that exists in the shared library. A user's personal copy on
// their shelf is represented by UserBook, which references one of these.
public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }

    public Author? Author { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string Isbn { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    [Display(Name = "Cover image URL")]
    public string? CoverImageUrl { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    // A book can belong to several distinct genres (e.g. science-fiction and fantasy).
    // The join table's composite key keeps a genre from being assigned to the same book twice.
    public ICollection<Genre> Genres { get; set; } = new List<Genre>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Personal shelf entries that reference this book.
    public ICollection<UserBook> UserBooks { get; set; } = new List<UserBook>();
}

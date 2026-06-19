using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models;

public class Genre
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    // Inverse side of the Book<->Genre many-to-many.
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models;

public class Author
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Url]
    [StringLength(2048)]
    public string? Photo { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();

    public ICollection<AuthorFollow> Followers { get; set; } = new List<AuthorFollow>();
}

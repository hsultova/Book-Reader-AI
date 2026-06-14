using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models;

// A user-created custom shelf. Books live on exactly one shelf — either a built-in
// ReadingStatus or a custom Shelf — so a UserBook references at most one of these.
public class Shelf
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

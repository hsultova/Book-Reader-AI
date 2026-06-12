using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models.ViewModels;

// Binds the Create/Edit book forms. Kept separate from the Book entity so view
// concerns and validation messaging don't leak into the domain model.
public class BookFormViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string Isbn { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    [Display(Name = "Cover image URL")]
    public string? CoverImageUrl { get; set; }

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [StringLength(60)]
    public string? Genre { get; set; }
}

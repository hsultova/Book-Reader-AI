using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models.ViewModels;

// Binds the Create/Edit book forms. Kept separate from the Book entity so view
// concerns and validation messaging don't leak into the domain model.
public class BookFormViewModel : IValidatableObject
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Author")]
    public string? AuthorValue { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(AuthorValue))
        {
            yield return new ValidationResult(
                "Author is required.",
                [nameof(AuthorValue)]);
        }
    }

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

    // Each entry is either an existing genre id or a free-typed genre name. A book may
    // carry several distinct genres.
    [Display(Name = "Genres")]
    public List<string> GenreValues { get; set; } = new();
}

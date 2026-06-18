using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models.ViewModels;

public class AuthorFormViewModel
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Url]
    [StringLength(2048)]
    [Display(Name = "Photo URL")]
    public string? Photo { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace BookReaderApp.Models;

// Where a book sits on a user's shelf.
public enum ReadingStatus
{
    [Display(Name = "Want to Read")]
    WantToRead = 0,

    [Display(Name = "Reading")]
    Reading = 1,

    [Display(Name = "Finished")]
    Finished = 2,
}

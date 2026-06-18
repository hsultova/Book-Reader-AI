namespace BookReaderApp.Models.ViewModels;

// A single "Because you enjoyed {Title}" row: a heading plus the books to suggest.
public sealed record RecommendationGroupViewModel(string Title, IReadOnlyList<Book> Books);

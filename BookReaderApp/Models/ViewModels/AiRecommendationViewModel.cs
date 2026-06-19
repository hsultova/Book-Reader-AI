namespace BookReaderApp.Models.ViewModels;

// A single AI-picked suggestion: the catalog book plus the personalized one-line reason the
// model gave for recommending it to this reader.
public sealed record AiRecommendationViewModel(Book Book, string Reason);

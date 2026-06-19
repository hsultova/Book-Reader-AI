using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Builds the AI-powered "Your AI Librarian suggests" section on the Books page: catalog books
// chosen and explained by Gemini based on what the reader has rated highly.
public interface IAiRecommendationService
{
    Task<IReadOnlyList<AiRecommendationViewModel>> GetAiRecommendationsAsync(
        string userId, CancellationToken ct = default);
}

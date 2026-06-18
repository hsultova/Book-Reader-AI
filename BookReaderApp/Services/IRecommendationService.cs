using BookReaderApp.Models.ViewModels;

namespace BookReaderApp.Services;

// Builds the personalized "Because you enjoyed..." rows shown on the Books page,
// derived from the books the user has rated highly.
public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationGroupViewModel>> GetRecommendationsAsync(string userId);
}

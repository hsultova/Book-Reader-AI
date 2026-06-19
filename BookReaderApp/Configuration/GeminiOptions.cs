namespace BookReaderApp.Configuration;

// Strongly-typed binding for the "Gemini" configuration section. The API key is loaded
// backend-only (appsettings.Local.json / environment) and never surfaced to the frontend —
// the Gemini call happens entirely server-side, like the Google Books integration.
public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    // The Gemini model used for recommendations. Can be pointed at another model
    // (e.g. "gemini-2.5-pro") via configuration without code changes.
    public string Model { get; set; } = "gemini-2.5-flash";

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    // Upper bound on how many AI suggestions to show on the Books page.
    public int MaxRecommendations { get; set; } = 6;
}

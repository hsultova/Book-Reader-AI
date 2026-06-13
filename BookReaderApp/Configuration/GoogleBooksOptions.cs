namespace BookReaderApp.Configuration;

// Strongly-typed binding for the "GoogleBooks" configuration section. The API key is loaded
// backend-only (appsettings.Local.json / environment) and never surfaced to the frontend.
public class GoogleBooksOptions
{
    public const string SectionName = "GoogleBooks";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://www.googleapis.com/books/v1/";
}

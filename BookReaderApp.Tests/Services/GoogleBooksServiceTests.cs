using System.Net;
using System.Text;
using BookReaderApp.Configuration;
using BookReaderApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BookReaderApp.Tests.Services;

public class GoogleBooksServiceTests
{
    // Returns a canned response (or throws) without touching the network.
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }

    private static GoogleBooksService NewService(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string apiKey = "test-key")
    {
        var http = new HttpClient(new StubHandler(responder));
        var options = Options.Create(new GoogleBooksOptions
        {
            ApiKey = apiKey,
            BaseUrl = "https://www.googleapis.com/books/v1/"
        });
        return new GoogleBooksService(http, options, NullLogger<GoogleBooksService>.Instance);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private const string SamplePayload = """
    {
      "items": [
        {
          "volumeInfo": {
            "title": "Clean Code",
            "authors": ["Robert C. Martin", "Co Author"],
            "description": "A handbook of agile software craftsmanship.",
            "categories": ["Computers", "Other"],
            "industryIdentifiers": [
              { "type": "ISBN_10", "identifier": "0132350882" },
              { "type": "ISBN_13", "identifier": "9780132350884" }
            ],
            "imageLinks": {
              "smallThumbnail": "http://books.google.com/small.jpg",
              "thumbnail": "http://books.google.com/thumb.jpg"
            }
          }
        }
      ]
    }
    """;

    [Fact]
    public async Task SearchAsync_WithMatches_MapsVolumeFieldsToResults()
    {
        var service = NewService(_ => Json(SamplePayload));

        var results = await service.SearchAsync("clean code");

        var book = Assert.Single(results);
        Assert.Equal("Clean Code", book.Title);
        Assert.Equal("Robert C. Martin", book.Author);
        Assert.Equal("9780132350884", book.Isbn); // ISBN_13 preferred over ISBN_10
        Assert.Equal("https://books.google.com/thumb.jpg", book.CoverImageUrl); // http -> https
        Assert.Equal("Computers", book.Genre);
        Assert.Equal("A handbook of agile software craftsmanship.", book.Description);
    }

    [Fact]
    public async Task SearchAsync_AttachesApiKeyToRequest()
    {
        string? capturedUri = null;
        var service = NewService(req =>
        {
            capturedUri = req.RequestUri!.ToString();
            return Json("""{ "items": [] }""");
        });

        await service.SearchAsync("anything");

        Assert.NotNull(capturedUri);
        Assert.Contains("key=test-key", capturedUri);
        Assert.Contains("q=anything", capturedUri);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyApiKey_ReturnsEmpty()
    {
        var called = false;
        var service = NewService(_ => { called = true; return Json(SamplePayload); }, apiKey: "");

        var results = await service.SearchAsync("clean code");

        Assert.Empty(results);
        Assert.False(called); // no API key -> no upstream call
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_ReturnsEmpty()
    {
        var called = false;
        var service = NewService(_ => { called = true; return Json(SamplePayload); });

        var results = await service.SearchAsync("   ");

        Assert.Empty(results);
        Assert.False(called);
    }

    [Fact]
    public async Task SearchAsync_WhenHttpFails_ReturnsEmptyAndDoesNotThrow()
    {
        var service = NewService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var results = await service.SearchAsync("clean code");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithNoItems_ReturnsEmpty()
    {
        var service = NewService(_ => Json("""{ "kind": "books#volumes", "totalItems": 0 }"""));

        var results = await service.SearchAsync("zzz no results");

        Assert.Empty(results);
    }
}

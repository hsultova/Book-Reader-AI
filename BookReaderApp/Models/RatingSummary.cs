namespace BookReaderApp.Models;

// Aggregate rating for a book across all users who rated it: the average star value
// and how many ratings it is based on. Computed on read; never persisted.
public record RatingSummary(double Average, int Count);

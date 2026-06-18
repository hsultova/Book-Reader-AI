using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookReaderApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing data may already contain duplicates (e.g. the same author entered
            // twice). Merge them onto a single canonical row — the lowest Id per
            // case-insensitive key — before adding the unique indexes, otherwise index
            // creation would fail on the duplicate rows.

            // --- Authors: collapse duplicate names ---
            // Drop author-follows that would collide once repointed (keep one per user).
            migrationBuilder.Sql(@"
                DELETE FROM AuthorFollows
                WHERE Id NOT IN (
                    SELECT MIN(af.Id) FROM AuthorFollows af
                    JOIN Authors a ON a.Id = af.AuthorId
                    GROUP BY af.UserId, lower(a.Name));");
            migrationBuilder.Sql(@"
                UPDATE AuthorFollows
                SET AuthorId = (
                    SELECT MIN(a.Id) FROM Authors a
                    WHERE lower(a.Name) = (SELECT lower(n.Name) FROM Authors n WHERE n.Id = AuthorFollows.AuthorId));");
            migrationBuilder.Sql(@"
                UPDATE Books
                SET AuthorId = (
                    SELECT MIN(a.Id) FROM Authors a
                    WHERE lower(a.Name) = (SELECT lower(n.Name) FROM Authors n WHERE n.Id = Books.AuthorId));");
            migrationBuilder.Sql(@"
                DELETE FROM Authors
                WHERE Id NOT IN (SELECT MIN(Id) FROM Authors GROUP BY lower(Name));");

            // --- Genres: collapse duplicate names ---
            migrationBuilder.Sql(@"
                UPDATE Books
                SET GenreId = (
                    SELECT MIN(g.Id) FROM Genres g
                    WHERE lower(g.Name) = (SELECT lower(n.Name) FROM Genres n WHERE n.Id = Books.GenreId))
                WHERE GenreId IS NOT NULL;");
            migrationBuilder.Sql(@"
                DELETE FROM Genres
                WHERE Id NOT IN (SELECT MIN(Id) FROM Genres GROUP BY lower(Name));");

            // --- Books: collapse duplicate ISBNs ---
            // Drop shelf entries / reviews that would collide once repointed (keep one per user).
            migrationBuilder.Sql(@"
                DELETE FROM UserBooks
                WHERE Id NOT IN (
                    SELECT MIN(ub.Id) FROM UserBooks ub
                    JOIN Books b ON b.Id = ub.BookId
                    GROUP BY ub.UserId, lower(b.Isbn));");
            migrationBuilder.Sql(@"
                UPDATE UserBooks
                SET BookId = (
                    SELECT MIN(b.Id) FROM Books b
                    WHERE lower(b.Isbn) = (SELECT lower(n.Isbn) FROM Books n WHERE n.Id = UserBooks.BookId));");
            migrationBuilder.Sql(@"
                DELETE FROM Reviews
                WHERE Id NOT IN (
                    SELECT MIN(r.Id) FROM Reviews r
                    JOIN Books b ON b.Id = r.BookId
                    GROUP BY r.UserId, lower(b.Isbn));");
            migrationBuilder.Sql(@"
                UPDATE Reviews
                SET BookId = (
                    SELECT MIN(b.Id) FROM Books b
                    WHERE lower(b.Isbn) = (SELECT lower(n.Isbn) FROM Books n WHERE n.Id = Reviews.BookId));");
            // Remove likes/comments left orphaned by the deleted reviews above.
            migrationBuilder.Sql(@"DELETE FROM ReviewLikes WHERE ReviewId NOT IN (SELECT Id FROM Reviews);");
            migrationBuilder.Sql(@"DELETE FROM ReviewComments WHERE ReviewId NOT IN (SELECT Id FROM Reviews);");
            migrationBuilder.Sql(@"
                DELETE FROM Books
                WHERE Id NOT IN (SELECT MIN(Id) FROM Books GROUP BY lower(Isbn));");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Name",
                table: "Authors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Genres_Name",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Books_Isbn",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Authors_Name",
                table: "Authors");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookReaderApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBookGenreManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the join table first so the existing single-genre assignments can be
            // copied across before the old GenreId column is dropped.
            migrationBuilder.CreateTable(
                name: "BookGenre",
                columns: table => new
                {
                    BooksId = table.Column<int>(type: "INTEGER", nullable: false),
                    GenresId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookGenre", x => new { x.BooksId, x.GenresId });
                    table.ForeignKey(
                        name: "FK_BookGenre_Books_BooksId",
                        column: x => x.BooksId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookGenre_Genres_GenresId",
                        column: x => x.GenresId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookGenre_GenresId",
                table: "BookGenre",
                column: "GenresId");

            // Preserve every book's current genre as its first many-to-many assignment.
            migrationBuilder.Sql(
                @"INSERT INTO ""BookGenre"" (""BooksId"", ""GenresId"")
                  SELECT ""Id"", ""GenreId"" FROM ""Books"" WHERE ""GenreId"" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Books_Genres_GenreId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_GenreId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "GenreId",
                table: "Books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenreId",
                table: "Books",
                type: "INTEGER",
                nullable: true);

            // Collapse the many-to-many back to a single genre per book (lowest genre id),
            // so no data is silently lost when rolling back.
            migrationBuilder.Sql(
                @"UPDATE ""Books"" SET ""GenreId"" = (
                      SELECT MIN(bg.""GenresId"") FROM ""BookGenre"" bg WHERE bg.""BooksId"" = ""Books"".""Id"");");

            migrationBuilder.DropTable(
                name: "BookGenre");

            migrationBuilder.CreateIndex(
                name: "IX_Books_GenreId",
                table: "Books",
                column: "GenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Genres_GenreId",
                table: "Books",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookReaderApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingToUserBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "UserBooks",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "UserBooks");
        }
    }
}

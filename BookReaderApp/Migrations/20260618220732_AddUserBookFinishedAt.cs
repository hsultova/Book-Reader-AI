using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookReaderApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBookFinishedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "UserBooks",
                type: "TEXT",
                nullable: true);

            // Backfill already-finished books (Status = Finished = 2) so existing data counts
            // toward the reading challenge, using the shelf-add time as the best-known finish.
            migrationBuilder.Sql(
                "UPDATE UserBooks SET FinishedAt = AddedAt WHERE Status = 2 AND FinishedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "UserBooks");
        }
    }
}

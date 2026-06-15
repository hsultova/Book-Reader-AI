using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookReaderApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRatedAtToUserBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RatedAt",
                table: "UserBooks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RatedAt",
                table: "UserBooks");
        }
    }
}

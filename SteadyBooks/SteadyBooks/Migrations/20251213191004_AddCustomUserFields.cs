using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteadyBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirmName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryContactEmail",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirmName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrimaryContactEmail",
                table: "AspNetUsers");
        }
    }
}

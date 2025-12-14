using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SteadyBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddClientDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientDashboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirmId = table.Column<string>(type: "text", nullable: false),
                    DashboardName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientCompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccessLink = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessPassword = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDashboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDashboards_AspNetUsers_FirmId",
                        column: x => x.FirmId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientDashboards_AccessLink",
                table: "ClientDashboards",
                column: "AccessLink",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientDashboards_FirmId",
                table: "ClientDashboards",
                column: "FirmId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientDashboards");
        }
    }
}

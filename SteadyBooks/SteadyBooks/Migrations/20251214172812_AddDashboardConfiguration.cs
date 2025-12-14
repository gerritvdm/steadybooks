using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SteadyBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientDashboardId = table.Column<int>(type: "integer", nullable: false),
                    DateRange = table.Column<int>(type: "integer", nullable: false),
                    CustomStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShowCashBalance = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProfit = table.Column<bool>(type: "boolean", nullable: false),
                    ShowTaxesDue = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOutstandingInvoices = table.Column<bool>(type: "boolean", nullable: false),
                    CashAccountMapping = table.Column<string>(type: "text", nullable: true),
                    TaxAccountMapping = table.Column<string>(type: "text", nullable: true),
                    CustomTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WelcomeMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardConfigurations_ClientDashboards_ClientDashboardId",
                        column: x => x.ClientDashboardId,
                        principalTable: "ClientDashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigurations_ClientDashboardId",
                table: "DashboardConfigurations",
                column: "ClientDashboardId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardConfigurations");
        }
    }
}

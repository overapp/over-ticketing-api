using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_TicketCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "dbo",
                table: "tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ticket_categories",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    background_color = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false, defaultValue: "#64748B"),
                    foreground_color = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false, defaultValue: "#FFFFFF"),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_ticket_categories_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "dbo",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tickets_category_id",
                schema: "dbo",
                table: "tickets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_categories_name",
                schema: "dbo",
                table: "ticket_categories",
                column: "name",
                unique: true,
                filter: "[project_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ticket_categories_project_id_name",
                schema: "dbo",
                table: "ticket_categories",
                columns: new[] { "project_id", "name" },
                unique: true,
                filter: "[project_id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_tickets_ticket_categories_category_id",
                schema: "dbo",
                table: "tickets",
                column: "category_id",
                principalSchema: "dbo",
                principalTable: "ticket_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tickets_ticket_categories_category_id",
                schema: "dbo",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "ticket_categories",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_tickets_category_id",
                schema: "dbo",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "dbo",
                table: "tickets");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Add_WikiPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wiki_pages",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    parent_page_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_internal_only = table.Column<bool>(type: "bit", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wiki_pages", x => x.id);
                    table.ForeignKey(
                        name: "fk_wiki_pages_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "dbo",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_wiki_pages_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wiki_pages_users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wiki_pages_wiki_pages_parent_page_id",
                        column: x => x.parent_page_id,
                        principalSchema: "dbo",
                        principalTable: "wiki_pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_created_by_user_id",
                schema: "dbo",
                table: "wiki_pages",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_parent_page_id",
                schema: "dbo",
                table: "wiki_pages",
                column: "parent_page_id");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_project_id_slug",
                schema: "dbo",
                table: "wiki_pages",
                columns: new[] { "project_id", "slug" },
                unique: true,
                filter: "[project_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_slug",
                schema: "dbo",
                table: "wiki_pages",
                column: "slug",
                unique: true,
                filter: "[project_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_updated_by_user_id",
                schema: "dbo",
                table: "wiki_pages",
                column: "updated_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wiki_pages",
                schema: "dbo");
        }
    }
}

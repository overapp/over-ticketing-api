using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class Remove_TodoItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "todo_items",
            schema: "dbo");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "todo_items",
            schema: "dbo",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                completed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                due_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                is_completed = table.Column<bool>(type: "bit", nullable: false),
                labels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                priority = table.Column<int>(type: "int", nullable: false),
                user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_todo_items", x => x.id);
                table.ForeignKey(
                    name: "fk_todo_items_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_todo_items_user_id",
            schema: "dbo",
            table: "todo_items",
            column: "user_id");
    }
}

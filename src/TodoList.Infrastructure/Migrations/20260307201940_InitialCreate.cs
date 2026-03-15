using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TodoList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsImportant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsInMyDay = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecurrenceType = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: true),
                    RecurrenceEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TodoListId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItems_TodoLists_TodoListId",
                        column: x => x.TodoListId,
                        principalTable: "TodoLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemCategory",
                columns: table => new
                {
                    CategoriesId = table.Column<int>(type: "integer", nullable: false),
                    TodoItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemCategory", x => new { x.CategoriesId, x.TodoItemId });
                    table.ForeignKey(
                        name: "FK_TodoItemCategory_Categories_CategoriesId",
                        column: x => x.CategoriesId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TodoItemCategory_TodoItems_TodoItemId",
                        column: x => x.TodoItemId,
                        principalTable: "TodoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemCategory_TodoItemId",
                table: "TodoItemCategory",
                column: "TodoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_DueDate",
                table: "TodoItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_IsCompleted",
                table: "TodoItems",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_IsImportant",
                table: "TodoItems",
                column: "IsImportant");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_IsInMyDay",
                table: "TodoItems",
                column: "IsInMyDay");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_TodoListId",
                table: "TodoItems",
                column: "TodoListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoItemCategory");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TodoItems");

            migrationBuilder.DropTable(
                name: "TodoLists");
        }
    }
}

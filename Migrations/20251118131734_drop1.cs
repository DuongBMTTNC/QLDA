using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLDA.Migrations
{
    /// <inheritdoc />
    public partial class drop1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_ProjectTask_TaskId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_TaskItems_TaskItemId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_ProjectTask_TaskId",
                table: "TaskFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropTable(
                name: "ProjectTask");

            migrationBuilder.DropIndex(
                name: "IX_TaskFiles_TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropIndex(
                name: "IX_TaskComments_TaskItemId",
                table: "TaskComments");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskComments");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_TaskItems_TaskId",
                table: "TaskComments",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskId",
                table: "TaskFiles",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_TaskItems_TaskId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskId",
                table: "TaskFiles");

            migrationBuilder.AddColumn<int>(
                name: "TaskItemId",
                table: "TaskFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskItemId",
                table: "TaskComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTask_AspNetUsers_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTask_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskFiles_TaskItemId",
                table: "TaskFiles",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskItemId",
                table: "TaskComments",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_AssignedUserId",
                table: "ProjectTask",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTask_ProjectId",
                table: "ProjectTask",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_ProjectTask_TaskId",
                table: "TaskComments",
                column: "TaskId",
                principalTable: "ProjectTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_TaskItems_TaskItemId",
                table: "TaskComments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_ProjectTask_TaskId",
                table: "TaskFiles",
                column: "TaskId",
                principalTable: "ProjectTask",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id");
        }
    }
}

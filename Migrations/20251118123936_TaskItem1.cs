using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLDA.Migrations
{
    /// <inheritdoc />
    public partial class TaskItem1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "TaskItems");

            migrationBuilder.RenameColumn(
                name: "Deadline",
                table: "TaskItems",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "TaskItems",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserId",
                table: "TaskItems",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskFiles_TaskItemId",
                table: "TaskFiles",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskItemId",
                table: "TaskComments",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskComments_TaskItems_TaskItemId",
                table: "TaskComments",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_AspNetUsers_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskComments_TaskItems_TaskItemId",
                table: "TaskComments");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_AspNetUsers_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskFiles_TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropIndex(
                name: "IX_TaskComments_TaskItemId",
                table: "TaskComments");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropColumn(
                name: "TaskItemId",
                table: "TaskComments");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "TaskItems",
                newName: "Deadline");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TaskItems",
                newName: "TaskId");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

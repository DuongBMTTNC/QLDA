using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLDA.Migrations
{
    /// <inheritdoc />
    public partial class TaskfileUD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskId",
                table: "TaskFiles");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "TaskFiles",
                newName: "TaskItemId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskFiles_TaskId",
                table: "TaskFiles",
                newName: "IX_TaskFiles_TaskItemId");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "TaskFiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UploadedById",
                table: "TaskFiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskFiles_UploadedById",
                table: "TaskFiles",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_AspNetUsers_UploadedById",
                table: "TaskFiles",
                column: "UploadedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_AspNetUsers_UploadedById",
                table: "TaskFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskItemId",
                table: "TaskFiles");

            migrationBuilder.DropIndex(
                name: "IX_TaskFiles_UploadedById",
                table: "TaskFiles");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "TaskFiles");

            migrationBuilder.DropColumn(
                name: "UploadedById",
                table: "TaskFiles");

            migrationBuilder.RenameColumn(
                name: "TaskItemId",
                table: "TaskFiles",
                newName: "TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskFiles_TaskItemId",
                table: "TaskFiles",
                newName: "IX_TaskFiles_TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskFiles_TaskItems_TaskId",
                table: "TaskFiles",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

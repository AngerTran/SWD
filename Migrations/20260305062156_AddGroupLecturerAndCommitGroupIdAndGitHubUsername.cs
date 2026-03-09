using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupLecturerAndCommitGroupIdAndGitHubUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commits_Sha",
                table: "Commits");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepo",
                table: "Groups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JiraProjectKey",
                table: "Groups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Commits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUsername",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupLecturers",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LecturerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupLecturers", x => new { x.GroupId, x.LecturerUserId });
                    table.ForeignKey(
                        name: "FK_GroupLecturers_AspNetUsers_LecturerUserId",
                        column: x => x.LecturerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupLecturers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_GroupId_Sha",
                table: "Commits",
                columns: new[] { "GroupId", "Sha" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_Sha",
                table: "Commits",
                column: "Sha",
                unique: true,
                filter: "[GroupId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GroupLecturers_LecturerUserId",
                table: "GroupLecturers",
                column: "LecturerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Commits_Groups_GroupId",
                table: "Commits",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commits_Groups_GroupId",
                table: "Commits");

            migrationBuilder.DropTable(
                name: "GroupLecturers");

            migrationBuilder.DropIndex(
                name: "IX_Commits_GroupId_Sha",
                table: "Commits");

            migrationBuilder.DropIndex(
                name: "IX_Commits_Sha",
                table: "Commits");

            migrationBuilder.DropColumn(
                name: "GitHubRepo",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "JiraProjectKey",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Commits");

            migrationBuilder.DropColumn(
                name: "GitHubUsername",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_Sha",
                table: "Commits",
                column: "Sha",
                unique: true);
        }
    }
}

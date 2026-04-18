using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectApprovalSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSpaceCollaboration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProposalId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectChatMessages_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectChatMessages_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProposalGroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProposalId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProposalGroupMembers_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalGroupMembers_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_ProposalId_SentAt",
                table: "ProjectChatMessages",
                columns: new[] { "ProposalId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_SenderId",
                table: "ProjectChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalGroupMembers_ProposalId_StudentId",
                table: "ProposalGroupMembers",
                columns: new[] { "ProposalId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProposalGroupMembers_StudentId",
                table: "ProposalGroupMembers",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectChatMessages");

            migrationBuilder.DropTable(
                name: "ProposalGroupMembers");
        }
    }
}

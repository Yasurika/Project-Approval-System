using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectApprovalSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupInvitationAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "ProposalGroupMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InviteStatus",
                table: "ProposalGroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "ProposalGroupMembers");

            migrationBuilder.DropColumn(
                name: "InviteStatus",
                table: "ProposalGroupMembers");
        }
    }
}

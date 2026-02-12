using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordSetupTokenAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "UserRegistrationRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "UserRegistrationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "UserRegistrationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UserRegistrationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PasswordSetupTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistrationRequestId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordSetupTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordSetupTokens_UserRegistrationRequests_RegistrationRequestId",
                        column: x => x.RegistrationRequestId,
                        principalTable: "UserRegistrationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Status",
                table: "UserRegistrationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordSetupTokens_RegistrationRequestId",
                table: "PasswordSetupTokens",
                column: "RegistrationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordSetupTokens_TokenHash",
                table: "PasswordSetupTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordSetupTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Status",
                table: "UserRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "UserRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "UserRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UserRegistrationRequests");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "UserRegistrationRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

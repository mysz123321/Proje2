using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordSetupTokens_UserRegistrationRequests_RegistrationRequestId",
                table: "PasswordSetupTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRegistrationRequests_Roles_RequestedRoleId",
                table: "UserRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRegistrationRequests_Users_ApprovedByUserId",
                table: "UserRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_PasswordSetupTokens_TokenHash",
                table: "PasswordSetupTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRegistrationRequests",
                table: "UserRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Status",
                table: "UserRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.RenameTable(
                name: "UserRegistrationRequests",
                newName: "RegistrationRequests");

            migrationBuilder.RenameIndex(
                name: "IX_UserRegistrationRequests_RequestedRoleId",
                table: "RegistrationRequests",
                newName: "IX_RegistrationRequests_RequestedRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRegistrationRequests_ApprovedByUserId",
                table: "RegistrationRequests",
                newName: "IX_RegistrationRequests_ApprovedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "PasswordSetupTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "Computers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "RegistrationRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "RegistrationRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RegistrationRequests",
                table: "RegistrationRequests",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_CreatedAt",
                table: "ComputerMetrics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordSetupTokens_RegistrationRequests_RegistrationRequestId",
                table: "PasswordSetupTokens",
                column: "RegistrationRequestId",
                principalTable: "RegistrationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Roles_RequestedRoleId",
                table: "RegistrationRequests",
                column: "RequestedRoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrationRequests_Users_ApprovedByUserId",
                table: "RegistrationRequests",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordSetupTokens_RegistrationRequests_RegistrationRequestId",
                table: "PasswordSetupTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Roles_RequestedRoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Users_ApprovedByUserId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ComputerMetrics_CreatedAt",
                table: "ComputerMetrics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RegistrationRequests",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests");

            migrationBuilder.RenameTable(
                name: "RegistrationRequests",
                newName: "UserRegistrationRequests");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrationRequests_RequestedRoleId",
                table: "UserRegistrationRequests",
                newName: "IX_UserRegistrationRequests_RequestedRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrationRequests_ApprovedByUserId",
                table: "UserRegistrationRequests",
                newName: "IX_UserRegistrationRequests_ApprovedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "PasswordSetupTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "Computers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "UserRegistrationRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserRegistrationRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRegistrationRequests",
                table: "UserRegistrationRequests",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Yönetici" },
                    { 2, new DateTime(2026, 2, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Denetleyici" },
                    { 3, new DateTime(2026, 2, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Görüntüleyici" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordSetupTokens_TokenHash",
                table: "PasswordSetupTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests",
                column: "Email",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Status",
                table: "UserRegistrationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests",
                column: "Username",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordSetupTokens_UserRegistrationRequests_RegistrationRequestId",
                table: "PasswordSetupTokens",
                column: "RegistrationRequestId",
                principalTable: "UserRegistrationRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRegistrationRequests_Roles_RequestedRoleId",
                table: "UserRegistrationRequests",
                column: "RequestedRoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRegistrationRequests_Users_ApprovedByUserId",
                table: "UserRegistrationRequests",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

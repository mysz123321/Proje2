using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyInterfaceAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropForeignKey(
                name: "FK_ComputerTags_Computers_ComputersId",
                table: "ComputerTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ComputerTags_Tags_TagsId",
                table: "ComputerTags");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Roles_RequestedRoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrationRequests_Users_ApprovedByUserId",
                table: "RegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RolesId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UsersId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_ApprovedByUserId",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_RequestedRoleId",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Computers");

            migrationBuilder.RenameColumn(
                name: "UsersId",
                table: "UserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "RolesId",
                table: "UserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_UsersId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "TagsId",
                table: "ComputerTags",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "ComputersId",
                table: "ComputerTags",
                newName: "ComputerId");

            migrationBuilder.RenameIndex(
                name: "IX_ComputerTags_TagsId",
                table: "ComputerTags",
                newName: "IX_ComputerTags_TagId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "UserRoles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "UserRoles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "UserRoles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tags",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestedRoleId",
                table: "RegistrationRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "RegistrationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "RegistrationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "RegistrationRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedDate",
                table: "RegistrationRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedBy",
                table: "RegistrationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "RegistrationRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ComputerTags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "ComputerTags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ComputerTags",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "ComputerTags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ComputerTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "FreeSpaceThresholdGb",
                table: "ComputerDisks",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ComputerDisks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "ComputerDisks",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerTags_Computers_ComputerId",
                table: "ComputerTags",
                column: "ComputerId",
                principalTable: "Computers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("DELETE FROM ComputerTags WHERE TagId NOT IN (SELECT Id FROM Tags)");
            migrationBuilder.Sql("DELETE FROM ComputerTags WHERE ComputerId NOT IN (SELECT Id FROM Computers)");

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerTags_Tags_TagId",
                table: "ComputerTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("DELETE FROM UserRoles WHERE RoleId NOT IN (SELECT Id FROM Roles)");
            migrationBuilder.Sql("DELETE FROM UserRoles WHERE UserId NOT IN (SELECT Id FROM Users)");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComputerTags_Computers_ComputerId",
                table: "ComputerTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ComputerTags_Tags_TagId",
                table: "ComputerTags");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "RegistrationRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ComputerTags");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ComputerTags");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ComputerTags");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ComputerTags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ComputerTags");

            migrationBuilder.DropColumn(
                name: "FreeSpaceThresholdGb",
                table: "ComputerDisks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ComputerDisks");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ComputerDisks");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "UserRoles",
                newName: "UsersId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UserRoles",
                newName: "RolesId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_UsersId");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "ComputerTags",
                newName: "TagsId");

            migrationBuilder.RenameColumn(
                name: "ComputerId",
                table: "ComputerTags",
                newName: "ComputersId");

            migrationBuilder.RenameIndex(
                name: "IX_ComputerTags_TagId",
                table: "ComputerTags",
                newName: "IX_ComputerTags_TagsId");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestedRoleId",
                table: "RegistrationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Computers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Computers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Computers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Computers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Computers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_ApprovedByUserId",
                table: "RegistrationRequests",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_RequestedRoleId",
                table: "RegistrationRequests",
                column: "RequestedRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerTags_Computers_ComputersId",
                table: "ComputerTags",
                column: "ComputersId",
                principalTable: "Computers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComputerTags_Tags_TagsId",
                table: "ComputerTags",
                column: "TagsId",
                principalTable: "Tags",
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
                name: "FK_UserRoles_Roles_RolesId",
                table: "UserRoles",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UsersId",
                table: "UserRoles",
                column: "UsersId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDescriptionAndReverseSidebar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SidebarItems_Permissions_RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.DropIndex(
                name: "IX_SidebarItems_RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Name",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "SidebarItemId",
                table: "Permissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_SidebarItemId",
                table: "Permissions",
                column: "SidebarItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_SidebarItems_SidebarItemId",
                table: "Permissions",
                column: "SidebarItemId",
                principalTable: "SidebarItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_SidebarItems_SidebarItemId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_SidebarItemId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "SidebarItemId",
                table: "Permissions");

            migrationBuilder.AddColumn<int>(
                name: "RequiredPermissionId",
                table: "SidebarItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_SidebarItems_RequiredPermissionId",
                table: "SidebarItems",
                column: "RequiredPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SidebarItems_Permissions_RequiredPermissionId",
                table: "SidebarItems",
                column: "RequiredPermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSidebarPermissionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredPermission",
                table: "SidebarItems");

            migrationBuilder.AddColumn<int>(
                name: "RequiredPermissionId",
                table: "SidebarItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SidebarItems_RequiredPermissionId",
                table: "SidebarItems",
                column: "RequiredPermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SidebarItems_Permissions_RequiredPermissionId",
                table: "SidebarItems",
                column: "RequiredPermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SidebarItems_Permissions_RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.DropIndex(
                name: "IX_SidebarItems_RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.DropColumn(
                name: "RequiredPermissionId",
                table: "SidebarItems");

            migrationBuilder.AddColumn<string>(
                name: "RequiredPermission",
                table: "SidebarItems",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

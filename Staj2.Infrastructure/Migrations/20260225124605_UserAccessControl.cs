using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleComputerAccesses");

            migrationBuilder.DropTable(
                name: "RoleTagAccesses");

            migrationBuilder.CreateTable(
                name: "UserComputerAccesses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ComputerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserComputerAccesses", x => new { x.UserId, x.ComputerId });
                    table.ForeignKey(
                        name: "FK_UserComputerAccesses_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserComputerAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTagAccesses",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTagAccesses", x => new { x.UserId, x.TagId });
                    table.ForeignKey(
                        name: "FK_UserTagAccesses_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTagAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserComputerAccesses_ComputerId",
                table: "UserComputerAccesses",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTagAccesses_TagId",
                table: "UserTagAccesses",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserComputerAccesses");

            migrationBuilder.DropTable(
                name: "UserTagAccesses");

            migrationBuilder.CreateTable(
                name: "RoleComputerAccesses",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ComputerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleComputerAccesses", x => new { x.RoleId, x.ComputerId });
                    table.ForeignKey(
                        name: "FK_RoleComputerAccesses_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleComputerAccesses_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleTagAccesses",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleTagAccesses", x => new { x.RoleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_RoleTagAccesses_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleTagAccesses_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleComputerAccesses_ComputerId",
                table: "RoleComputerAccesses",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleTagAccesses_TagId",
                table: "RoleTagAccesses",
                column: "TagId");
        }
    }
}

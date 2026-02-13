using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComputerTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- SADECE BUNLAR KALACAK (Yeni Tablolar) ---

            migrationBuilder.CreateTable(
                name: "Computers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MacAddress = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CpuModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TotalRamMb = table.Column<double>(type: "float", nullable: false),
                    TotalDiskGb = table.Column<double>(type: "float", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Computers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComputerMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<int>(type: "int", nullable: false),
                    CpuUsage = table.Column<double>(type: "float", nullable: false),
                    RamUsage = table.Column<double>(type: "float", nullable: false),
                    DiskUsage = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerMetrics_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_ComputerId",
                table: "ComputerMetrics",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_Computers_MacAddress",
                table: "Computers",
                column: "MacAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComputerMetrics");

            migrationBuilder.DropTable(
                name: "PasswordSetupTokens");

            migrationBuilder.DropTable(
                name: "Computers");

            migrationBuilder.DropTable(
                name: "UserRegistrationRequests");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}

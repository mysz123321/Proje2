using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropThresholdHistoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComputerThresholdHistories");

            migrationBuilder.DropTable(
                name: "DiskThresholdHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComputerThresholdHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<int>(type: "int", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CpuThreshold = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    RamThreshold = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerThresholdHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerThresholdHistories_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiskThresholdHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerDiskId = table.Column<int>(type: "int", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ThresholdPercent = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskThresholdHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiskThresholdHistories_ComputerDisks_ComputerDiskId",
                        column: x => x.ComputerDiskId,
                        principalTable: "ComputerDisks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerThresholdHistories_ComputerId_ActiveFrom",
                table: "ComputerThresholdHistories",
                columns: new[] { "ComputerId", "ActiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_DiskThresholdHistories_ComputerDiskId_ActiveFrom",
                table: "DiskThresholdHistories",
                columns: new[] { "ComputerDiskId", "ActiveFrom" });
        }
    }
}

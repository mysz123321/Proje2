using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThresholdsAndDiskTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalDiskGb",
                table: "Computers");

            migrationBuilder.AddColumn<double>(
                name: "CpuThreshold",
                table: "Computers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RamThreshold",
                table: "Computers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "ComputerDisks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<int>(type: "int", nullable: false),
                    DiskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalSizeGb = table.Column<double>(type: "float", nullable: false),
                    ThresholdPercent = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerDisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComputerDisks_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiskMetrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerDiskId = table.Column<int>(type: "int", nullable: false),
                    UsedPercent = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiskMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiskMetrics_ComputerDisks_ComputerDiskId",
                        column: x => x.ComputerDiskId,
                        principalTable: "ComputerDisks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerDisks_ComputerId",
                table: "ComputerDisks",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_ComputerDiskId",
                table: "DiskMetrics",
                column: "ComputerDiskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiskMetrics");

            migrationBuilder.DropTable(
                name: "ComputerDisks");

            migrationBuilder.DropColumn(
                name: "CpuThreshold",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "RamThreshold",
                table: "Computers");

            migrationBuilder.AddColumn<string>(
                name: "TotalDiskGb",
                table: "Computers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

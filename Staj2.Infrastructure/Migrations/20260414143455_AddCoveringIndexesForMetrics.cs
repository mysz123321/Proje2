using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveringIndexesForMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics",
                columns: new[] { "ComputerDiskId", "CreatedAt" })
                .Annotation("SqlServer:Include", new[] { "UsedPercent" });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics",
                columns: new[] { "ComputerId", "CreatedAt" })
                .Annotation("SqlServer:Include", new[] { "CpuUsage", "RamUsage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics",
                columns: new[] { "ComputerDiskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics",
                columns: new[] { "ComputerId", "CreatedAt" });
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMetricIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_ComputerDiskId",
                table: "DiskMetrics");

            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_CreatedAt",
                table: "DiskMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ComputerMetrics_CreatedAt",
                table: "ComputerMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics",
                columns: new[] { "ComputerDiskId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_ComputerDiskId_CreatedAt",
                table: "DiskMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_ComputerDiskId",
                table: "DiskMetrics",
                column: "ComputerDiskId");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_CreatedAt",
                table: "DiskMetrics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_CreatedAt",
                table: "ComputerMetrics",
                column: "CreatedAt");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_ComputerMetrics_ComputerId",
            //    table: "ComputerMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_DiskMetrics_CreatedAt",
                table: "DiskMetrics",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics",
                columns: new[] { "ComputerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiskMetrics_CreatedAt",
                table: "DiskMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ComputerMetrics_ComputerId_CreatedAt",
                table: "ComputerMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerMetrics_ComputerId",
                table: "ComputerMetrics",
                column: "ComputerId");
        }
    }
}

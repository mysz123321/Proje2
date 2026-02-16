using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldDiskUsageField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiskUsage",
                table: "ComputerMetrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiskUsage",
                table: "ComputerMetrics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

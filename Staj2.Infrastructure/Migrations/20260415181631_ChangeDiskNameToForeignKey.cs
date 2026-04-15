using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDiskNameToForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiskName",
                table: "MetricWarningLogs");

            migrationBuilder.AddColumn<int>(
                name: "ComputerDiskId",
                table: "MetricWarningLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricWarningLogs_ComputerDiskId",
                table: "MetricWarningLogs",
                column: "ComputerDiskId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetricWarningLogs_ComputerDisks_ComputerDiskId",
                table: "MetricWarningLogs",
                column: "ComputerDiskId",
                principalTable: "ComputerDisks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetricWarningLogs_ComputerDisks_ComputerDiskId",
                table: "MetricWarningLogs");

            migrationBuilder.DropIndex(
                name: "IX_MetricWarningLogs_ComputerDiskId",
                table: "MetricWarningLogs");

            migrationBuilder.DropColumn(
                name: "ComputerDiskId",
                table: "MetricWarningLogs");

            migrationBuilder.AddColumn<string>(
                name: "DiskName",
                table: "MetricWarningLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}

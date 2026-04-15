using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricTypeLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetricWarningLogs_ComputerId_MetricType_CreatedAt",
                table: "MetricWarningLogs");

            migrationBuilder.DropColumn(
                name: "MetricType",
                table: "MetricWarningLogs");

            migrationBuilder.AddColumn<int>(
                name: "MetricTypeId",
                table: "MetricWarningLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MetricTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MetricTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "CPU" },
                    { 2, "RAM" },
                    { 3, "Disk" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricWarningLogs_ComputerId_MetricTypeId_CreatedAt",
                table: "MetricWarningLogs",
                columns: new[] { "ComputerId", "MetricTypeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricWarningLogs_MetricTypeId",
                table: "MetricWarningLogs",
                column: "MetricTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MetricWarningLogs_MetricTypes_MetricTypeId",
                table: "MetricWarningLogs",
                column: "MetricTypeId",
                principalTable: "MetricTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MetricWarningLogs_MetricTypes_MetricTypeId",
                table: "MetricWarningLogs");

            migrationBuilder.DropTable(
                name: "MetricTypes");

            migrationBuilder.DropIndex(
                name: "IX_MetricWarningLogs_ComputerId_MetricTypeId_CreatedAt",
                table: "MetricWarningLogs");

            migrationBuilder.DropIndex(
                name: "IX_MetricWarningLogs_MetricTypeId",
                table: "MetricWarningLogs");

            migrationBuilder.DropColumn(
                name: "MetricTypeId",
                table: "MetricWarningLogs");

            migrationBuilder.AddColumn<string>(
                name: "MetricType",
                table: "MetricWarningLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MetricWarningLogs_ComputerId_MetricType_CreatedAt",
                table: "MetricWarningLogs",
                columns: new[] { "ComputerId", "MetricType", "CreatedAt" });
        }
    }
}

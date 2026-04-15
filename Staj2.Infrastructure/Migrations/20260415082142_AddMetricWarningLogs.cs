using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricWarningLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetricWarningLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComputerId = table.Column<int>(type: "int", nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MetricValue = table.Column<double>(type: "float", nullable: false),
                    ThresholdValue = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricWarningLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricWarningLogs_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricWarningLogs_ComputerId_MetricType_CreatedAt",
                table: "MetricWarningLogs",
                columns: new[] { "ComputerId", "MetricType", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricWarningLogs");
        }
    }
}

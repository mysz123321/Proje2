using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitNotificationTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastNotifyTime",
                table: "Computers",
                newName: "RamLastNotifyTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "CpuLastNotifyTime",
                table: "Computers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotifyTime",
                table: "ComputerDisks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuLastNotifyTime",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "LastNotifyTime",
                table: "ComputerDisks");

            migrationBuilder.RenameColumn(
                name: "RamLastNotifyTime",
                table: "Computers",
                newName: "LastNotifyTime");
        }
    }
}

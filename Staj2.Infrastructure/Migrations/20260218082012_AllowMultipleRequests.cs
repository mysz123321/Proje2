using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests",
                column: "Username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Email",
                table: "RegistrationRequests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRequests_Username",
                table: "RegistrationRequests",
                column: "Username",
                unique: true);
        }
    }
}

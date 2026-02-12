using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Staj2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueIndexesForRegistrationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests",
                column: "Email",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests",
                column: "Username",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Email",
                table: "UserRegistrationRequests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistrationRequests_Username",
                table: "UserRegistrationRequests",
                column: "Username",
                unique: true);
        }
    }
}

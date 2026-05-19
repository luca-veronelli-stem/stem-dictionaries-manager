using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiActiveCredentialPerInstallationGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials");

            migrationBuilder.CreateIndex(
                name: "UX_InstallationApiCredentials_Active",
                table: "InstallationApiCredentials",
                column: "InstallationId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_InstallationApiCredentials_Active",
                table: "InstallationApiCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials",
                column: "InstallationId");
        }
    }
}

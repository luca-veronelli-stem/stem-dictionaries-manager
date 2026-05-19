using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MultiCredentialPerInstallationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials",
                column: "InstallationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials",
                column: "InstallationId",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBootstrapRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OsUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MachineId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstallGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescriptorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BootstrapTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MintedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedByInstallationId = table.Column<int>(type: "int", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BootstrapTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BootstrapTokens_Installations_ConsumedByInstallationId",
                        column: x => x.ConsumedByInstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallationApiCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallationId = table.Column<int>(type: "int", nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallationApiCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallationApiCredentials_Installations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedClientApp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClaimedOsUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClaimedMachineId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClaimedInstallGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedAppVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    DescriptorJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ResultingInstallationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrationEvents_Installations_ResultingInstallationId",
                        column: x => x.ResultingInstallationId,
                        principalTable: "Installations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BootstrapTokens_ConsumedByInstallationId",
                table: "BootstrapTokens",
                column: "ConsumedByInstallationId",
                unique: true,
                filter: "[ConsumedByInstallationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BootstrapTokens_SecretHash",
                table: "BootstrapTokens",
                column: "SecretHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BootstrapTokens_Status",
                table: "BootstrapTokens",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InstallationApiCredentials_InstallationId",
                table: "InstallationApiCredentials",
                column: "InstallationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallationApiCredentials_SecretHash",
                table: "InstallationApiCredentials",
                column: "SecretHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installations_ClientApp_OsUserId_MachineId",
                table: "Installations",
                columns: new[] { "ClientApp", "OsUserId", "MachineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Installations_InstallGuid",
                table: "Installations",
                column: "InstallGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEvents_ClaimedClientApp_OccurredAt",
                table: "RegistrationEvents",
                columns: new[] { "ClaimedClientApp", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEvents_OccurredAt",
                table: "RegistrationEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEvents_ResultingInstallationId",
                table: "RegistrationEvents",
                column: "ResultingInstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEvents_SourceIp",
                table: "RegistrationEvents",
                column: "SourceIp");

            // Seed the system-admin user (data-model.md § Audit split): admin
            // API-key callers do not have an organic per-request User, so the
            // AdminAuthenticationMiddleware sets ICurrentUserProvider.CurrentUserId
            // to this row's id whenever an AdminApiKeys hit lands.
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Username", "DisplayName", "CreatedAt", "UpdatedAt" },
                values: new object[]
                {
                    "system-admin",
                    "System Admin (API key)",
                    new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc),
                    DBNull.Value
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Username",
                keyValue: "system-admin");

            migrationBuilder.DropTable(
                name: "BootstrapTokens");

            migrationBuilder.DropTable(
                name: "InstallationApiCredentials");

            migrationBuilder.DropTable(
                name: "RegistrationEvents");

            migrationBuilder.DropTable(
                name: "Installations");
        }
    }
}

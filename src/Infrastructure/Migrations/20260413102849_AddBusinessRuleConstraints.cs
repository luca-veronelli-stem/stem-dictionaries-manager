using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessRuleConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boards_DeviceId",
                table: "Boards");

            migrationBuilder.CreateIndex(
                name: "IX_Dictionaries_IsStandard",
                table: "Dictionaries",
                column: "IsStandard",
                unique: true,
                filter: "[IsStandard] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Devices_MachineCode",
                table: "Devices",
                sql: "[MachineCode] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Commands_Name",
                table: "Commands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boards_DeviceId",
                table: "Boards",
                column: "DeviceId",
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BitInterpretations_BitIndex",
                table: "BitInterpretations",
                sql: "[BitIndex] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BitInterpretations_WordIndex",
                table: "BitInterpretations",
                sql: "[WordIndex] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dictionaries_IsStandard",
                table: "Dictionaries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Devices_MachineCode",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Commands_Name",
                table: "Commands");

            migrationBuilder.DropIndex(
                name: "IX_Boards_DeviceId",
                table: "Boards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BitInterpretations_BitIndex",
                table: "BitInterpretations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BitInterpretations_WordIndex",
                table: "BitInterpretations");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_DeviceId",
                table: "Boards",
                column: "DeviceId");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTypeToDictionary_RemoveDeviceTypeFromBitInterpretation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BitInterpretations_VariableId_DeviceType_WordIndex_BitIndex",
                table: "BitInterpretations");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "BitInterpretations");

            migrationBuilder.AddColumn<int>(
                name: "DeviceType",
                table: "Dictionaries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Meaning",
                table: "BitInterpretations",
                type: "TEXT",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_Dictionaries_DeviceType_BoardTypeId",
                table: "Dictionaries",
                columns: new[] { "DeviceType", "BoardTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitInterpretations_VariableId_WordIndex_BitIndex",
                table: "BitInterpretations",
                columns: new[] { "VariableId", "WordIndex", "BitIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dictionaries_DeviceType_BoardTypeId",
                table: "Dictionaries");

            migrationBuilder.DropIndex(
                name: "IX_BitInterpretations_VariableId_WordIndex_BitIndex",
                table: "BitInterpretations");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Dictionaries");

            migrationBuilder.AlterColumn<string>(
                name: "Meaning",
                table: "BitInterpretations",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceType",
                table: "BitInterpretations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BitInterpretations_VariableId_DeviceType_WordIndex_BitIndex",
                table: "BitInterpretations",
                columns: new[] { "VariableId", "DeviceType", "WordIndex", "BitIndex" },
                unique: true);
        }
    }
}

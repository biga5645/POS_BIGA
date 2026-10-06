using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_BIGA.Migrations
{
    /// <inheritdoc />
    public partial class FixPinColumnLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ توسيع عمود Pin لاستيعاب BCrypt hash (60 حرف)
            migrationBuilder.AlterColumn<string>(
                name: "Pin",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Credits",
                keyColumn: "Id",
                keyValue: 1,
                column: "Notes",
                value: " دين تجريبي أولي");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "FullName",
                value: "مدير النظام ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Pin",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Credits",
                keyColumn: "Id",
                keyValue: 1,
                column: "Notes",
                value: "دين تجريبي أولي");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "FullName",
                value: "مدير النظام");
        }
    }
}

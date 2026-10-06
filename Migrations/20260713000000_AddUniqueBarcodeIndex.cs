using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS_BIGA.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBarcodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ جعل فهرس الباركود فريداً لمنع تكرار نفس الباركود بين منتجين مختلفين.
            // نستثني الباركود الفارغ "" من قيد التفرد (HasFilter) لأن أكثر من منتج
            // قد لا يملك باركود بعد، ولا يجب أن يمنعهم ذلك من الحفظ.
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");
        }
    }
}

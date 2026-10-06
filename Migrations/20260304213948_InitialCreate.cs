using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace POS_BIGA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SettingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ShopName = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    ShopAddress = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    ShopPhone = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "MAD"),
                    CreditEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    InvoiceMessage = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDivisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SubUnitName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubUnitQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false, defaultValue: 1m),
                    AllowCustomPrice = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Cash"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Completed"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Cashier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegisterId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false, defaultValue: 0m),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,3)", nullable: false, defaultValue: 0m),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Credits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credits_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credits_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SaleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductBarcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    LineTotalAfterDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<int>(type: "int", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    QuantityBefore = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SaleId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Credits_CreditId",
                        column: x => x.CreditId,
                        principalTable: "Credits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_Sales_SaleId1",
                        column: x => x.SaleId1,
                        principalTable: "Sales",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "Category", "CreatedAt", "Currency", "Description", "InvoiceMessage", "IsEditable", "SettingKey", "SettingType", "SettingValue", "ShopAddress", "ShopName", "ShopPhone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "General", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MAD", "اسم المتجر", "", true, "ShopName", "String", "متجري", "", "متجري", "", null },
                    { 2, "Sales", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MAD", "نسبة الضريبة الافتراضية", "", true, "TaxRate", "Decimal", "0.00", "", "", "", null },
                    { 3, "Hardware", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MAD", "الطابعة الافتراضية للإيصالات", "", true, "ReceiptPrinter", "String", "", "", "", "", null }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "ParentCategoryId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "مواد غذائية متنوعة", "", true, "مواد غدائية", null, null },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "مشروبات ساخنة وباردة", "", true, "مشروبات", null, null },
                    { 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "أدوات مكتبية", "", true, "مكتب", null, null }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "CreatedAt", "CreatedBy", "CreditLimit", "Email", "FullName", "IsActive", "Notes", "Phone", "UpdatedAt" },
                values: new object[] { 1, "الرباط", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1000m, "test@example.com", "عميل تجريبي", true, "زبون تجريبي", "0612345678", null });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "AllowCustomPrice", "CreatedAt", "IsActive", "IsDivisible", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[] { 1, "kg", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "كيلوجرام", "غرام", 1000m });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "AllowCustomPrice", "CreatedAt", "IsActive", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[] { 2, "g", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "غرام", null, 1m });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "AllowCustomPrice", "CreatedAt", "IsActive", "IsDivisible", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[,]
                {
                    { 3, "m", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "متر", "سنتيمتر", 100m },
                    { 4, "L", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "لتر", "مليلتر", 1000m }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "CreatedAt", "IsActive", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[] { 5, "piece", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "قطعة", null, 1m });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "CreatedAt", "IsActive", "IsDivisible", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[,]
                {
                    { 6, "box", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "كرتونة", "قطعة", 12m },
                    { 7, "dozen", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "دزينة", "قطعة", 12m },
                    { 8, "can", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "علبة", "قطعة", 24m }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "AllowCustomPrice", "CreatedAt", "IsActive", "IsDivisible", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[] { 9, "ton", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "طن", "كيلوجرام", 1000m });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "CreatedAt", "IsActive", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[,]
                {
                    { 10, "btl", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "زجاجة", null, 1m },
                    { 11, "unit", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "حبة", null, 1m }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "Id", "Abbreviation", "CreatedAt", "IsActive", "IsDivisible", "Name", "SubUnitName", "SubUnitQuantity" },
                values: new object[] { 12, "bag", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "كيس", "قطعة", 50m });

            migrationBuilder.InsertData(
                table: "Credits",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "CustomerId", "DueDate", "Notes", "OriginalAmount", "RemainingAmount", "SaleId", "Status", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 1, null, "دين تجريبي أولي", 250m, 250m, null, 0, null });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CategoryId", "CreatedAt", "CreatedBy", "CurrentStock", "Description", "ImageUrl", "IsActive", "MinimumStock", "Name", "PurchasePrice", "SKU", "SalePrice", "UnitId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "1001", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 50m, "قلم تسجيل عالي الجودة", "https://via.placeholder.com/150?text=Pen", true, 10m, "قلم تسجيل", 0.90m, "PEN-001", 1.50m, 1, null },
                    { 2, "1002", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 30m, "دفتر ملاحظات A5", "https://via.placeholder.com/150?text=Notebook", true, 5m, "دفتر ملاحظات", 2.20m, "NTB-001", 3.75m, 1, null },
                    { 3, "1003", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 100m, "ماء معدني 500 مل", "https://via.placeholder.com/150?text=Water", true, 20m, "زجاجة ماء", 0.40m, "WTR-001", 0.90m, 10, null },
                    { 4, "1004", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 80m, "ممحاة ناعمة", "https://via.placeholder.com/150?text=Eraser", true, 15m, "ممحاة", 0.20m, "ERS-001", 0.50m, 1, null },
                    { 5, "1005", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", 20m, "سكين مكتبي معدني", "https://via.placeholder.com/150?text=Knife", true, 5m, "سكين مكتبي", 1.50m, "KNF-001", 2.20m, 1, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_Category",
                table: "AppSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_SettingKey",
                table: "AppSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_CustomerId",
                table: "Credits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_DueDate",
                table: "Credits",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_SaleId",
                table: "Credits",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_Status",
                table: "Credits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsActive",
                table: "Customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreditId",
                table: "Payments",
                column: "CreditId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentDate",
                table: "Payments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethod",
                table: "Payments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleId1",
                table: "Payments",
                column: "SaleId1");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_ProductId",
                table: "SaleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PaymentType",
                table: "Sales",
                column: "PaymentType");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleNumber",
                table: "Sales",
                column: "SaleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementType",
                table: "StockMovements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReferenceType_ReferenceId",
                table: "StockMovements",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SaleId",
                table: "StockMovements",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "Credits");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}

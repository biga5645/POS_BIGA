using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Infrastructure.Printing.Templates;
using POS_BIGA.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PdfColors = QuestPDF.Helpers.Colors;
using PdfColor = QuestPDF.Infrastructure.Color;

namespace POS_BIGA.Infrastructure.Printing.Pdf
{
    /// <summary>
    /// 📊 بناء تقارير PDF احترافية (ملخص، فواتير، ديون) باستخدام QuestPDF.
    /// يُستخدم من شاشة التقارير عند الضغط على أزرار "تصدير PDF".
    /// </summary>
    public static class ReportQuestPdfBuilder
    {
        static ReportQuestPdfBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ─── مشترك: رأس الصفحة ────────────────────────────────────────────────

        private static void ComposeHeader(QuestPDF.Infrastructure.IContainer container, PrintSettings s, string title, string subtitle)
        {
            container.Column(col =>
            {
                col.Item()
                    .Background(PdfColors.Blue.Darken3)
                    .Padding(12)
                    .Column(inner =>
                    {
                        if (!string.IsNullOrWhiteSpace(s?.ShopName))
                        {
                            inner.Item().AlignCenter()
                                .Text(s.ShopName).FontSize(18).Bold().FontColor(PdfColors.White);
                        }

                        inner.Item().AlignCenter()
                            .Text(title).FontSize(14).FontColor(PdfColors.White);
                    });

                if (!string.IsNullOrEmpty(s?.ShopAddress) || !string.IsNullOrEmpty(s?.ShopPhone))
                {
                    col.Item()
                        .Background(PdfColors.Blue.Lighten4)
                        .Padding(6)
                        .Row(row =>
                        {
                            row.RelativeItem().AlignRight()
                                .Text(s?.ShopAddress ?? string.Empty)
                                .FontSize(9).FontColor(PdfColors.Grey.Darken2);

                            row.RelativeItem().AlignLeft()
                                .Text(string.IsNullOrEmpty(s?.ShopPhone) ? string.Empty : "هاتف: " + s.ShopPhone)
                                .FontSize(9).FontColor(PdfColors.Grey.Darken2);
                        });
                }

                if (!string.IsNullOrEmpty(subtitle))
                {
                    col.Item()
                        .BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                        .Padding(6)
                        .AlignCenter()
                        .Text(subtitle).FontSize(10).FontColor(PdfColors.Grey.Darken2);
                }
            });
        }

        private static void ComposeFooter(QuestPDF.Infrastructure.IContainer container)
        {
            container
                .BorderTop(1).BorderColor(PdfColors.Blue.Lighten3)
                .Padding(8)
                .Row(row =>
                {
                    row.RelativeItem().AlignRight()
                        .Text(x =>
                        {
                            x.Span("صفحة ").FontSize(9).FontColor(PdfColors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(9).FontColor(PdfColors.Grey.Medium);
                            x.Span(" / ").FontSize(9).FontColor(PdfColors.Grey.Medium);
                            x.TotalPages().FontSize(9).FontColor(PdfColors.Grey.Medium);
                        });

                    row.RelativeItem().AlignLeft()
                        .Text($"طُبع: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(PdfColors.Grey.Medium);
                });
        }

        private static string Fmt(decimal v, int dp, string cur) => v.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur;

        // ════════════════════════════════════════════════════════════════════
        // 1) تقرير الملخص الشامل
        // ════════════════════════════════════════════════════════════════════

        public static byte[] BuildSummaryPdfBytes(ReportData data, PrintSettings settings)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            settings ??= new PrintSettings();

            int dp = settings.DecimalPlaces;
            string cur = settings.CurrencySymbol ?? settings.Currency ?? "MAD";
            bool isA4 = settings.PaperSize != PaperSize.A5;

            bool secSummary = settings.A4ReportShowSummary;
            bool secCredits = settings.A4ReportShowCredits;
            bool secCreditsList = settings.A4ReportShowCreditsList;
            bool secTopProducts = settings.A4ReportShowTopProducts;
            bool secCategories = settings.A4ReportShowCategories;
            bool secDailySales = settings.A4ReportShowDailySales;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(isA4 ? PageSizes.A4 : PageSizes.A5);
                    page.Margin(isA4 ? 30 : 20);
                    page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(isA4 ? 10 : 9).DirectionFromRightToLeft());

                    page.Header().Element(c => ComposeHeader(c, settings, "تقرير المبيعات",
                        $"الفترة: {data.StartDate:yyyy-MM-dd} إلى {data.EndDate:yyyy-MM-dd}"));

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(12);

                        if (secSummary)
                        {
                            col.Item().Text("📊 ملخص الأداء").FontSize(13).Bold().FontColor(PdfColors.Blue.Darken3);

                            var stats = new (string Label, string Value)[]
                            {
                                ("إجمالي المبيعات", Fmt(data.TotalSales, dp, cur)),
                                ("إجمالي الأرباح", Fmt(data.TotalProfit, dp, cur)),
                                ("عدد الفواتير", data.InvoicesCount.ToString()),
                                ("معدل الفاتورة", Fmt(data.AverageSale, dp, cur)),
                                ("هامش الربح", $"{data.ProfitMarginPercent:N1}%"),
                                ("نمو المبيعات", data.SalesGrowthFormatted),
                            };

                            col.Item().Row(row =>
                            {
                                for (int i = 0; i < stats.Length; i++)
                                {
                                    var (label, value) = stats[i];
                                    row.RelativeItem().Padding(2).Background(PdfColors.Grey.Lighten5)
                                        .Border(1).BorderColor(PdfColors.Grey.Lighten2)
                                        .Padding(8).Column(c2 =>
                                        {
                                            c2.Item().Text(label).FontSize(8).FontColor(PdfColors.Grey.Darken1);
                                            c2.Item().Text(value).FontSize(11).Bold().FontColor(PdfColors.Blue.Darken2);
                                        });
                                }
                            });
                        }

                        if (secCredits && data.TotalCredits > 0)
                        {
                            col.Item().Text("💳 ملخص الديون").FontSize(13).Bold().FontColor(PdfColors.Blue.Darken3);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn();
                                });

                                void H(string t) => table.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                    .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                H("إجمالي الديون"); H("المدفوع"); H("المتبقي"); H("نسبة التسديد");

                                void C(string t, string color = null) => table.Cell().Padding(6).AlignCenter()
                                    .Text(t).FontSize(9).FontColor(color != null ? PdfColor.FromHex(color) : PdfColors.Black);
                                C(Fmt(data.TotalCredits, dp, cur));
                                C(Fmt(data.PaidCredits, dp, cur), "#27AE60");
                                C(Fmt(data.RemainingCredits, dp, cur), "#E74C3C");
                                C($"{data.CreditPaymentRate:N1}%");
                            });

                            if (secCreditsList && data.CreditCustomers?.Count > 0)
                            {
                                col.Item().PaddingTop(4).Table(table =>
                                {
                                    table.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(); });

                                    table.Header(h =>
                                    {
                                        h.Cell().Background(PdfColors.Grey.Darken2).Padding(6).AlignCenter()
                                            .Text("الزبون").Bold().FontColor(PdfColors.White).FontSize(9);
                                        h.Cell().Background(PdfColors.Grey.Darken2).Padding(6).AlignCenter()
                                            .Text("المتبقي").Bold().FontColor(PdfColors.White).FontSize(9);
                                    });

                                    int i = 0;
                                    foreach (var c in data.CreditCustomers.OrderByDescending(x => x.RemainingAmount))
                                    {
                                        var bg = i % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                                        table.Cell().Background(bg).Padding(6).AlignRight().Text(c.CustomerName).FontSize(9);
                                        table.Cell().Background(bg).Padding(6).AlignCenter().Text(Fmt(c.RemainingAmount, dp, cur)).FontSize(9);
                                        i++;
                                    }
                                });
                            }
                        }

                        if (secTopProducts && data.TopProducts?.Count > 0)
                        {
                            col.Item().Text("🏆 أفضل 10 منتجات").FontSize(13).Bold().FontColor(PdfColors.Blue.Darken3);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(28); cols.RelativeColumn(3); cols.RelativeColumn(); cols.RelativeColumn();
                                });

                                table.Header(h =>
                                {
                                    void H(string t) => h.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                        .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                    H("#"); H("المنتج"); H("الكمية"); H("الإيرادات");
                                });

                                int rank = 1;
                                foreach (var p in data.TopProducts.Take(10))
                                {
                                    var bg = rank % 2 == 0 ? PdfColors.Grey.Lighten5 : PdfColors.White;
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(rank.ToString()).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignRight().Text(p.Name ?? string.Empty).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(p.QuantitySold.ToString($"N{dp}")).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(Fmt(p.TotalRevenue, dp, cur)).FontSize(9);
                                    rank++;
                                }
                            });
                        }

                        if (secCategories && data.CategorySales?.Count > 0)
                        {
                            col.Item().Text("📂 مبيعات الفئات").FontSize(13).Bold().FontColor(PdfColors.Blue.Darken3);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(); cols.RelativeColumn(); });

                                table.Header(h =>
                                {
                                    void H(string t) => h.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                        .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                    H("الفئة"); H("الإيرادات"); H("النسبة");
                                });

                                int i = 0;
                                foreach (var c in data.CategorySales.OrderByDescending(x => x.TotalRevenue))
                                {
                                    var bg = i % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                                    table.Cell().Background(bg).Padding(6).AlignRight().Text(c.CategoryName ?? string.Empty).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(Fmt(c.TotalRevenue, dp, cur)).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text($"{c.Percentage:N1}%").FontSize(9);
                                    i++;
                                }
                            });
                        }

                        if (secDailySales && data.DailySales?.Count > 0)
                        {
                            col.Item().Text("📅 المبيعات اليومية").FontSize(13).Bold().FontColor(PdfColors.Blue.Darken3);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });

                                table.Header(h =>
                                {
                                    void H(string t) => h.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                        .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                    H("التاريخ"); H("الفواتير"); H("الإجمالي");
                                });

                                int i = 0;
                                foreach (var d in data.DailySales.OrderByDescending(x => x.Date))
                                {
                                    var bg = i % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(d.Date.ToString("yyyy-MM-dd")).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(d.InvoicesCount.ToString()).FontSize(9);
                                    table.Cell().Background(bg).Padding(6).AlignCenter().Text(Fmt(d.TotalSales, dp, cur)).FontSize(9);
                                    i++;
                                }
                            });
                        }
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return doc.GeneratePdf();
        }

        public static void SaveSummaryPdf(ReportData data, PrintSettings settings, string filePath)
        {
            var bytes = BuildSummaryPdfBytes(data, settings);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            File.WriteAllBytes(filePath, bytes);
        }

        // ════════════════════════════════════════════════════════════════════
        // 2) قائمة الفواتير خلال فترة
        // ════════════════════════════════════════════════════════════════════

        public static byte[] BuildInvoicesListPdfBytes(List<SaleReportDto> sales, DateTime start, DateTime end, PrintSettings settings)
        {
            sales ??= new List<SaleReportDto>();
            settings ??= new PrintSettings();

            int dp = settings.DecimalPlaces;
            string cur = settings.CurrencySymbol ?? settings.Currency ?? "MAD";

            decimal totalAmount = sales.Sum(s => s.TotalAmount);
            decimal totalPaid = sales.Sum(s => s.PaidAmount);
            decimal totalRemaining = sales.Sum(s => s.RemainingAmount);
            decimal totalProfit = sales.Sum(s => s.TotalProfit);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(9).DirectionFromRightToLeft());

                    page.Header().Element(c => ComposeHeader(c, settings, "تقرير الفواتير",
                        $"الفترة: {start:yyyy-MM-dd} إلى {end:yyyy-MM-dd}   |   عدد الفواتير: {sales.Count}"));

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.2f);  // رقم الفاتورة
                                cols.RelativeColumn(1.1f);  // التاريخ
                                cols.RelativeColumn(1.4f);  // الزبون
                                cols.RelativeColumn(0.6f);  // الأصناف
                                cols.RelativeColumn(1f);    // طريقة الدفع
                                cols.RelativeColumn(1f);    // الإجمالي
                                cols.RelativeColumn(1f);    // المدفوع
                                cols.RelativeColumn(1f);    // المتبقي
                                cols.RelativeColumn(1f);    // الربح
                            });

                            table.Header(h =>
                            {
                                void H(string t) => h.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                    .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                H("رقم الفاتورة"); H("التاريخ"); H("الزبون"); H("الأصناف");
                                H("طريقة الدفع"); H("الإجمالي"); H("المدفوع"); H("المتبقي"); H("الربح");
                            });

                            int i = 0;
                            foreach (var s in sales.OrderByDescending(x => x.SaleDate))
                            {
                                var bg = i % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                                void C(string t, string color = null) => table.Cell().Background(bg).Padding(5).AlignCenter()
                                    .Text(t ?? string.Empty).FontSize(8).FontColor(color != null ? PdfColor.FromHex(color) : PdfColors.Black);

                                C(s.SaleNumber);
                                C(s.SaleDate.ToString("yyyy-MM-dd HH:mm"));
                                C(s.CustomerName);
                                C(s.ItemsCount.ToString());
                                C(s.PaymentMethod);
                                C(Fmt(s.TotalAmount, dp, cur));
                                C(Fmt(s.PaidAmount, dp, cur), "#27AE60");
                                C(Fmt(s.RemainingAmount, dp, cur), s.RemainingAmount > 0 ? "#E74C3C" : null);
                                C(Fmt(s.TotalProfit, dp, cur));
                                i++;
                            }
                        });

                        col.Item().PaddingTop(10).AlignLeft().Width(320).Table(table =>
                        {
                            table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });

                            void Row(string label, string value, bool bold = false)
                            {
                                var lt = table.Cell().Padding(4).AlignRight().Text(label).FontSize(9).FontColor(PdfColors.Grey.Darken2);
                                var vt = table.Cell().Padding(4).AlignLeft().Text(value).FontSize(bold ? 11 : 9);
                                if (bold) { vt.Bold(); }
                            }

                            Row("إجمالي المبالغ:", Fmt(totalAmount, dp, cur), bold: true);
                            Row("إجمالي المدفوع:", Fmt(totalPaid, dp, cur));
                            Row("إجمالي المتبقي:", Fmt(totalRemaining, dp, cur));
                            Row("إجمالي الربح:", Fmt(totalProfit, dp, cur));
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return doc.GeneratePdf();
        }

        public static void SaveInvoicesListPdf(List<SaleReportDto> sales, DateTime start, DateTime end, PrintSettings settings, string filePath)
        {
            var bytes = BuildInvoicesListPdfBytes(sales, start, end, settings);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            File.WriteAllBytes(filePath, bytes);
        }

        // ════════════════════════════════════════════════════════════════════
        // 3) قائمة الديون
        // ════════════════════════════════════════════════════════════════════

        public static byte[] BuildCreditsListPdfBytes(List<CreditReportDto> credits, PrintSettings settings)
        {
            credits ??= new List<CreditReportDto>();
            settings ??= new PrintSettings();

            int dp = settings.DecimalPlaces;
            string cur = settings.CurrencySymbol ?? settings.Currency ?? "MAD";

            decimal totalOriginal = credits.Sum(c => c.OriginalAmount);
            decimal totalPaid = credits.Sum(c => c.PaidAmount);
            decimal totalRemaining = credits.Sum(c => c.RemainingAmount);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(9).DirectionFromRightToLeft());

                    page.Header().Element(c => ComposeHeader(c, settings, "تقرير الديون",
                        $"عدد الديون: {credits.Count}   |   تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd}"));

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(1.5f); // الزبون
                                cols.RelativeColumn(1f);   // الفاتورة
                                cols.RelativeColumn(1f);   // تاريخ الإنشاء
                                cols.RelativeColumn(1f);   // تاريخ الاستحقاق
                                cols.RelativeColumn(1f);   // الأصلي
                                cols.RelativeColumn(1f);   // المدفوع
                                cols.RelativeColumn(1f);   // المتبقي
                                cols.RelativeColumn(1f);   // الحالة
                            });

                            table.Header(h =>
                            {
                                void H(string t) => h.Cell().Background(PdfColors.Blue.Darken3).Padding(6)
                                    .AlignCenter().Text(t).Bold().FontColor(PdfColors.White).FontSize(9);
                                H("الزبون"); H("الفاتورة"); H("تاريخ الإنشاء"); H("تاريخ الاستحقاق");
                                H("الأصلي"); H("المدفوع"); H("المتبقي"); H("الحالة");
                            });

                            int i = 0;
                            foreach (var c in credits.OrderByDescending(x => x.CreatedDate))
                            {
                                var bg = i % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                                void C(string t, string color = null) => table.Cell().Background(bg).Padding(5).AlignCenter()
                                    .Text(t ?? string.Empty).FontSize(8).FontColor(color != null ? PdfColor.FromHex(color) : PdfColors.Black);

                                C(c.CustomerName);
                                C(c.SaleNumber);
                                C(c.CreatedDateFormatted);
                                C(c.DueDateFormatted);
                                C(Fmt(c.OriginalAmount, dp, cur));
                                C(Fmt(c.PaidAmount, dp, cur), "#27AE60");
                                C(Fmt(c.RemainingAmount, dp, cur), c.RemainingAmount > 0 ? "#E74C3C" : null);
                                C(c.Status, c.StatusColor);
                                i++;
                            }
                        });

                        col.Item().PaddingTop(10).AlignLeft().Width(320).Table(table =>
                        {
                            table.ColumnsDefinition(cols => { cols.RelativeColumn(); cols.RelativeColumn(); });

                            void Row(string label, string value, bool bold = false)
                            {
                                var lt = table.Cell().Padding(4).AlignRight().Text(label).FontSize(9).FontColor(PdfColors.Grey.Darken2);
                                var vt = table.Cell().Padding(4).AlignLeft().Text(value).FontSize(bold ? 11 : 9);
                                if (bold) { vt.Bold(); }
                            }

                            Row("إجمالي الديون:", Fmt(totalOriginal, dp, cur), bold: true);
                            Row("إجمالي المدفوع:", Fmt(totalPaid, dp, cur));
                            Row("إجمالي المتبقي:", Fmt(totalRemaining, dp, cur));
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return doc.GeneratePdf();
        }

        public static void SaveCreditsListPdf(List<CreditReportDto> credits, PrintSettings settings, string filePath)
        {
            var bytes = BuildCreditsListPdfBytes(credits, settings);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            File.WriteAllBytes(filePath, bytes);
        }
    }
}

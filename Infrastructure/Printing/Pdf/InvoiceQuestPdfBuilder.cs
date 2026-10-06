using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.Linq;
using PdfColors = QuestPDF.Helpers.Colors;
using PdfColor = QuestPDF.Infrastructure.Color;

namespace POS_BIGA.Infrastructure.Printing.Pdf
{
    /// <summary>
    /// بناء فاتورة PDF احترافية باستخدام QuestPDF — نفس القالب للطباعة والمعاينة والتصدير.
    /// </summary>
    public static class InvoiceQuestPdfBuilder
    {
        static InvoiceQuestPdfBuilder()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// بناء فاتورة PDF وإرجاعها كـ byte[].
        /// آمن للاستدعاء من أي thread.
        /// </summary>
        public static byte[] BuildPdfBytes(Sale sale, PrintSettings settings)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            settings ??= new PrintSettings();

            sale.TaxRate = settings.TaxRate;
            sale.CalculateTotals();

            var data = LoadExtraData(sale);

            return Document.Create(c => ComposeDocument(c, sale, settings, data))
                           .GeneratePdf();
        }

        /// <summary>
        /// حفظ فاتورة PDF على القرص.
        /// </summary>
        public static void SavePdf(Sale sale, PrintSettings settings, string filePath)
        {
            var bytes = BuildPdfBytes(sale, settings);
            System.IO.Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filePath))!);
            System.IO.File.WriteAllBytes(filePath, bytes);
        }

        // ─── Data Loading ─────────────────────────────────────────────────────────

        private record ExtraData(string CustomerName, decimal PreviousDebt);

        private static ExtraData LoadExtraData(Sale sale)
        {
            if (!sale.CustomerId.HasValue || sale.CustomerId.Value <= 0)
                return new ExtraData(string.Empty, 0);

            try
            {
                using var ctx = new AppDbContext();
                var customer = ctx.Customers.AsNoTracking()
                    .FirstOrDefault(c => c.Id == sale.CustomerId.Value);
                if (customer == null) return new ExtraData(string.Empty, 0);

                decimal debt = ctx.Credits
                    .Where(c => c.CustomerId == sale.CustomerId.Value
                             && c.RemainingAmount > 0
                             && c.SaleId != sale.Id)
                    .Sum(c => (decimal?)c.RemainingAmount) ?? 0;

                return new ExtraData(customer.FullName, debt);
            }
            catch
            {
                return new ExtraData(string.Empty, 0);
            }
        }

        // ─── Document Composition ─────────────────────────────────────────────────

        private static void ComposeDocument(IDocumentContainer container, Sale sale,
            PrintSettings s, ExtraData extra)
        {
            bool isA4 = s.PaperSize != PaperSize.A5;
            var pageSize = isA4 ? PageSizes.A4 : PageSizes.A5;
            float margin = isA4 ? 30 : 20;
            float bodyFs = isA4 ? 10f : 9f;
            float titleFs = isA4 ? 22f : 18f;
            float sectionFs = isA4 ? 12f : 10f;

            int dp = s.DecimalPlaces;
            string cur = s.Currency ?? "MAD";

            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(margin);
                page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(bodyFs)
                                              .DirectionFromRightToLeft());

                // ── Header ──────────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    // Shop name banner
                    col.Item()
                        .Background(PdfColors.Blue.Darken3)
                        .Padding(12)
                        .AlignCenter()
                        .Text(s.ShopName)
                        .FontSize(titleFs).Bold().FontColor(PdfColors.White);

                    if (!string.IsNullOrEmpty(s.ShopAddress) || !string.IsNullOrEmpty(s.ShopPhone))
                    {
                        col.Item()
                            .Background(PdfColors.Blue.Lighten4)
                            .Padding(6)
                            .Row(row =>
                            {
                                row.RelativeItem().AlignRight()
                                    .Text(s.ShopAddress ?? string.Empty)
                                    .FontSize(bodyFs - 1).FontColor(PdfColors.Grey.Darken2);

                                row.RelativeItem().AlignLeft()
                                    .Text(string.IsNullOrEmpty(s.ShopPhone) ? string.Empty : "Tel: " + s.ShopPhone)
                                    .FontSize(bodyFs - 1).FontColor(PdfColors.Grey.Darken2);
                            });
                    }

                    // Invoice info bar
                    col.Item()
                        .BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                        .Padding(8)
                        .Row(row =>
                        {
                            row.RelativeItem().AlignRight().Column(c2 =>
                            {
                                c2.Item().Text("فاتورة رقم").FontSize(bodyFs - 1)
                                    .FontColor(PdfColors.Grey.Medium);
                                c2.Item().Text(sale.SaleNumber ?? $"#{sale.Id}")
                                    .FontSize(sectionFs).Bold();
                            });

                            row.ConstantItem(1).Background(PdfColors.Grey.Lighten3);

                            row.RelativeItem().AlignLeft().Column(c2 =>
                            {
                                c2.Item().Text("التاريخ").FontSize(bodyFs - 1)
                                    .FontColor(PdfColors.Grey.Medium);
                                c2.Item().Text(sale.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"))
                                    .FontSize(sectionFs).Bold();
                            });
                        });
                });

                // ── Content ──────────────────────────────────────────────────────
                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // Customer info
                    if (!string.IsNullOrEmpty(extra.CustomerName))
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Background(PdfColors.Grey.Lighten4)
                                .Padding(8).Column(c2 =>
                                {
                                    c2.Item().Text("العميل").FontSize(bodyFs - 1)
                                        .FontColor(PdfColors.Grey.Medium);
                                    c2.Item().Text(extra.CustomerName).Bold().FontSize(sectionFs);
                                });

                            if (extra.PreviousDebt > 0)
                            {
                                row.ConstantItem(8);
                                row.RelativeItem().Background(PdfColors.Red.Lighten4)
                                    .Padding(8).Column(c2 =>
                                    {
                                        c2.Item().Text("الدين السابق").FontSize(bodyFs - 1)
                                            .FontColor(PdfColors.Red.Darken2);
                                        c2.Item()
                                            .Text(extra.PreviousDebt.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur)
                                            .Bold().FontSize(sectionFs).FontColor(PdfColors.Red.Darken2);
                                    });
                            }
                        });
                    }

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);   // الصنف
                            cols.RelativeColumn(2);   // الكمية × السعر
                            cols.RelativeColumn(2);   // الإجمالي
                        });

                        // Table header
                        table.Header(h =>
                        {
                            void HeaderCell(string text) =>
                                h.Cell().Background(PdfColors.Blue.Darken3)
                                    .Padding(8).AlignCenter().AlignMiddle()
                                    .Text(text).Bold().FontColor(PdfColors.White).FontSize(10);

                            HeaderCell("الصنف");
                            HeaderCell("الكمية × السعر");
                            HeaderCell("الإجمالي");
                        });

                        // Items
                        int rowIndex = 0;
                        foreach (var item in sale.Items)
                        {
                            var bg = rowIndex % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;

                            var name = item.Name ?? string.Empty;
                            var qtyPrice = $"{item.Quantity:N2} × {item.UnitPrice.ToString($"N{dp}", CultureInfo.InvariantCulture)}";
                            var total = item.LineTotal.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur;

                            table.Cell().Background(bg).Padding(7).AlignRight()
                                .Text(name).FontSize(bodyFs);
                            table.Cell().Background(bg).Padding(7).AlignCenter()
                                .Text(qtyPrice).FontSize(bodyFs);
                            table.Cell().Background(bg).Padding(7).AlignLeft()
                                .Text(total).FontSize(bodyFs).Bold();

                            rowIndex++;
                        }
                    });

                    // Totals section
                    col.Item().AlignLeft().Width(isA4 ? 220 : 180).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        void TotalRow(string label, string value,
                            bool bold = false, string labelColor = null, string valueColor = null)
                        {
                            var lc = labelColor != null
                                ? QuestPDF.Infrastructure.Color.FromHex(labelColor) : PdfColors.Grey.Darken2;
                            var vc = valueColor != null
                                ? QuestPDF.Infrastructure.Color.FromHex(valueColor) : PdfColors.Black;

                            var lt = table.Cell().Padding(4).AlignRight()
                                .Text(label).FontSize(bodyFs).FontColor(lc);
                            if (bold) lt.Bold();

                            var vt = table.Cell().Padding(4).AlignLeft()
                                .Text(value).FontSize(bold ? sectionFs : bodyFs).FontColor(vc);
                            if (bold) vt.Bold();
                        }

                        TotalRow("المجموع الفرعي:", sale.SubTotal.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur);

                        if (sale.Tax > 0)
                            TotalRow($"الضريبة ({sale.TaxRate}%):", sale.Tax.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur);

                        if (sale.DiscountAmount > 0)
                            TotalRow("الخصم:", "-" + sale.DiscountAmount.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur,
                                valueColor: "#E74C3C");

                        // Divider row
                        table.Cell().ColumnSpan(2).BorderTop(1).BorderColor(PdfColors.Grey.Lighten2)
                            .PaddingTop(2);

                        TotalRow("الإجمالي:", sale.Total.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur,
                            bold: true, valueColor: "#1A5276");

                        // Payments
                        if (sale.Payments?.Count > 0)
                        {
                            foreach (var p in sale.Payments)
                            {
                                string method = p.Method switch
                                {
                                    PaymentMethod.Cash => "نقداً",
                                    PaymentMethod.Card => "بطاقة",
                                    PaymentMethod.Mobile => "هاتف",
                                    PaymentMethod.Voucher => "قسيمة",
                                    _ => "مدفوع"
                                };
                                TotalRow(method + ":", p.Amount.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur);
                            }
                        }
                        else if (sale.AmountPaid > 0)
                        {
                            TotalRow("المدفوع:", sale.AmountPaid.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur);
                        }

                        if (sale.AmountPaid > sale.Total)
                        {
                            TotalRow("الباقي:", (sale.AmountPaid - sale.Total).ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur);
                        }
                        else if (sale.AmountDue > 0)
                        {
                            TotalRow("المتبقي:", sale.AmountDue.ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur,
                                bold: true, valueColor: "#E74C3C");
                        }

                        // Total debt including previous
                        if (extra.PreviousDebt > 0 && sale.AmountDue > 0)
                        {
                            table.Cell().ColumnSpan(2).PaddingTop(4);
                            TotalRow("إجمالي الدين:", (extra.PreviousDebt + sale.AmountDue).ToString($"N{dp}", CultureInfo.InvariantCulture) + " " + cur,
                                bold: true, labelColor: "#922B21", valueColor: "#922B21");
                        }
                    });
                });

                // ── Footer ──────────────────────────────────────────────────────
                page.Footer()
                    .BorderTop(1).BorderColor(PdfColors.Blue.Lighten3)
                    .Padding(10)
                    .Column(col =>
                    {
                        if (!string.IsNullOrEmpty(s.InvoiceMessage))
                        {
                            col.Item().AlignCenter()
                                .Text(s.InvoiceMessage)
                                .FontSize(bodyFs).Italic().FontColor(PdfColors.Grey.Darken2);
                        }

                        col.Item().AlignCenter()
                            .Text($"طُبع: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(bodyFs - 1).FontColor(PdfColors.Grey.Medium);
                    });
            });
        }
    }
}

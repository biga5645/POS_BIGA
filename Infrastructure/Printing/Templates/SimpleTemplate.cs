// استبدل كل محتوى SimpleTemplate.cs بهذا:

using POS_BIGA.Infrastructure.Printing.Abstractions;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data;

namespace POS_BIGA.Infrastructure.Printing.Templates
{
    public class SimpleTemplate : IPrintTemplate
    {
        public string Name => "Simple";

        public FlowDocument BuildPreview(Sale sale, PrintSettings shopSettings)
        {
            if (sale == null)
                return new FlowDocument(new Paragraph(new Run("No sale")));

            sale.TaxRate = shopSettings?.TaxRate ?? 0m;
            sale.CalculateTotals();

            int dp = shopSettings?.DecimalPlaces ?? 2;
            string currency = shopSettings?.Currency ?? "MAD";

            // استخدام الإعدادات الحرارية
            double receiptWidthMm = shopSettings?.ReceiptWidthMm > 0 ? shopSettings.ReceiptWidthMm : 80;
            double pageWidth = receiptWidthMm * (96.0 / 25.4);
            int baseFontSize = shopSettings?.ThermalFontSize > 0 ? shopSettings.ThermalFontSize : 12;
            bool showHeader = shopSettings?.ShowThermalHeader ?? true;
            bool showLines = shopSettings?.ShowThermalLines ?? true;

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = baseFontSize,
                PagePadding = new Thickness(6),
                PageWidth = pageWidth,
                FlowDirection = FlowDirection.RightToLeft
            };

            doc.ColumnWidth = pageWidth - doc.PagePadding.Left - doc.PagePadding.Right;

            /* ================= HEADER ================= */

            if (showHeader)
            {
                doc.Blocks.Add(new Paragraph(new Run(shopSettings?.ShopName ?? ""))
                {
                    FontSize = baseFontSize + 4,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                });

            //// ✅ Add "En Gros" in bold
            //    doc.Blocks.Add(new Paragraph(new Run("En Gros"))
            //    {
            //        FontSize = baseFontSize + 1,
            //        FontWeight = FontWeights.Bold,
            //        TextAlignment = TextAlignment.Center,
            //        Margin = new Thickness(0, 2, 0, 4),
            //        Foreground = Brushes.Black
            //    });

                if (!string.IsNullOrEmpty(shopSettings?.ShopAddress))
                    doc.Blocks.Add(new Paragraph(new Run(shopSettings.ShopAddress))
                    {
                        FontSize = baseFontSize - 1,
                        TextAlignment = TextAlignment.Center
                    });

                if (!string.IsNullOrEmpty(shopSettings?.ShopPhone))
                    doc.Blocks.Add(new Paragraph(new Run("Tel: " + shopSettings.ShopPhone))
                    {
                        FontSize = baseFontSize - 1,
                        TextAlignment = TextAlignment.Center
                    });
            }

            if (shopSettings?.ShowDateTime != false)
                doc.Blocks.Add(new Paragraph(new Run(
                    sale.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm")))
                {
                    FontSize = baseFontSize - 1,
                    TextAlignment = TextAlignment.Center
                });

            if (showLines)
                doc.Blocks.Add(Separator());

            /* ================= PREVIOUS DEBT (if any) ================= */

            decimal previousDebt = 0;
            string customerName = "";

            if (sale.CustomerId.HasValue && sale.CustomerId.Value > 0)
            {
                try
                {
                    using var ctx = new AppDbContext();
                    var customer = ctx.Customers.AsNoTracking()
                        .FirstOrDefault(c => c.Id == sale.CustomerId.Value);

                    if (customer != null)
                    {
                        customerName = customer.FullName;
                        previousDebt = ctx.Credits
                            .Where(c => c.CustomerId == sale.CustomerId.Value
                                   && c.RemainingAmount > 0
                                   && c.SaleId != sale.Id)
                            .Sum(c => (decimal?)c.RemainingAmount) ?? 0;
                    }
                }
                catch { }
            }

            if (previousDebt > 0 && !string.IsNullOrEmpty(customerName))
            {
                doc.Blocks.Add(new Paragraph(new Run("معلومات العميل"))
                {
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.DarkRed,
                    Margin = new Thickness(0, 6, 0, 2)
                });

                doc.Blocks.Add(new Paragraph(new Run($"العميل: {customerName}"))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 2)
                });

                var previousDebtPara = new Paragraph
                {
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                previousDebtPara.Inlines.Add(new Run("الدين السابق: ")
                { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed });
                previousDebtPara.Inlines.Add(new Run(previousDebt.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency)
                { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed });
                doc.Blocks.Add(previousDebtPara);

                doc.Blocks.Add(Separator());
            }

            /* ================= ITEMS TABLE ================= */

            // ✅ Table ensures perfect alignment for mixed Arabic/English
            var table = new Table
            {
                CellSpacing = 0,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 4, 10, 4), // ✅ Shifted left: 10px right margin
                FontSize = 9
            };

            // ✅ Optimized column widths to fit paper better
            table.Columns.Add(new TableColumn { Width = new GridLength(120) }); // الصنف (was 140)
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // الكمية (was 90)
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // الإجمالي (was 90)

            var tableRowGroup = new TableRowGroup();
            table.RowGroups.Add(tableRowGroup);

            // Header
            var headerRow = new TableRow { FontWeight = FontWeights.Bold };
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الصنف"))
            { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FontSize = 10 })
            { Padding = new Thickness(2), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.Gray });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الكمية"))
            { Margin = new Thickness(0), TextAlignment = TextAlignment.Center, FontSize = 10 })
            { Padding = new Thickness(2), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.Gray });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الإجمالي"))
            { Margin = new Thickness(0), TextAlignment = TextAlignment.Left, FontSize = 10 })
            { Padding = new Thickness(2), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.Gray });
            tableRowGroup.Rows.Add(headerRow);

            // Items
            foreach (var it in sale.Items)
            {
                string name = it.Name ?? "";
                if (name.Length > 18) // ✅ Reduced to fit narrower column (was 20)
                    name = name.Substring(0, 17) + "…";

                string qtyPrice = $"{it.Quantity}×{it.UnitPrice.ToString($"N{dp}", CultureInfo.CurrentCulture)}";
                string total = $"{it.LineTotal.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}";

                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(name))
                { Margin = new Thickness(0), TextAlignment = TextAlignment.Right })
                { Padding = new Thickness(2) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(qtyPrice))
                { Margin = new Thickness(0), TextAlignment = TextAlignment.Center })
                { Padding = new Thickness(2) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(total))
                { Margin = new Thickness(0), TextAlignment = TextAlignment.Left })
                { Padding = new Thickness(2) });

                tableRowGroup.Rows.Add(row);
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(Separator());

            /* ================= TOTALS ================= */

            var subtotalPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
            subtotalPara.Inlines.Add(new Run("المجموع: ") { FontWeight = FontWeights.Normal });
            subtotalPara.Inlines.Add(new Run(sale.SubTotal.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
            doc.Blocks.Add(subtotalPara);

            if (sale.Tax > 0)
            {
                var taxPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                taxPara.Inlines.Add(new Run("الضريبة: "));
                taxPara.Inlines.Add(new Run(sale.Tax.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
                doc.Blocks.Add(taxPara);
            }

            var totalPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
            totalPara.Inlines.Add(new Run("الإجمالي: ") { FontWeight = FontWeights.Bold });
            totalPara.Inlines.Add(new Run(sale.Total.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(totalPara);

            // Payments & change
            try
            {
                if (sale.Payments != null && sale.Payments.Count > 0)
                {
                    foreach (var p in sale.Payments)
                    {
                        string methodStr = p.Method switch
                        {
                            PaymentMethod.Cash => "نقداً",
                            PaymentMethod.Card => "بطاقة",
                            PaymentMethod.Mobile => "هاتف",
                            PaymentMethod.Voucher => "قسيمة",
                            _ => "مدفوع"
                        };
                        var payPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                        payPara.Inlines.Add(new Run(methodStr + ": "));
                        payPara.Inlines.Add(new Run(p.Amount.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
                        doc.Blocks.Add(payPara);
                    }
                }
                else if (sale.AmountPaid > 0)
                {
                    var paidPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                    paidPara.Inlines.Add(new Run("المدفوع: "));
                    paidPara.Inlines.Add(new Run(sale.AmountPaid.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
                    doc.Blocks.Add(paidPara);
                }

                if (sale.AmountPaid > sale.Total)
                {
                    var ch = sale.AmountPaid - sale.Total;
                    var changePara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                    changePara.Inlines.Add(new Run("الباقي: "));
                    changePara.Inlines.Add(new Run(ch.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
                    doc.Blocks.Add(changePara);
                }
                else if (sale.AmountDue > 0)
                {
                    var duePara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                    duePara.Inlines.Add(new Run("المتبقي: "));
                    duePara.Inlines.Add(new Run(sale.AmountDue.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency));
                    doc.Blocks.Add(duePara);
                }
            }
            catch { }

            /* ================= TOTAL DEBT ================= */

            if (previousDebt > 0 && sale.AmountDue > 0)
            {
                doc.Blocks.Add(Separator());

                var currentDebtPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                currentDebtPara.Inlines.Add(new Run("الدين من هذه الفاتورة: ")
                { FontWeight = FontWeights.Normal, Foreground = Brushes.DarkOrange });
                currentDebtPara.Inlines.Add(new Run(sale.AmountDue.ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency)
                { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkOrange });
                doc.Blocks.Add(currentDebtPara);

                var totalDebtPara = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 2, 0, 2) };
                totalDebtPara.Inlines.Add(new Run("إجمالي الدين الكلي: ")
                { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed, FontSize = 12 });
                totalDebtPara.Inlines.Add(new Run((previousDebt + sale.AmountDue).ToString($"N{dp}", CultureInfo.CurrentCulture) + " " + currency)
                { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed, FontSize = 12 });
                doc.Blocks.Add(totalDebtPara);
            }

            /* ================= FOOTER ================= */

            doc.Blocks.Add(new Paragraph(new Run(shopSettings?.InvoiceMessage ?? "شكراً لزيارتكم"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            });

            return doc;
        }

        public byte[] BuildPdf(Sale sale, PrintSettings shopSettings)
            => POS_BIGA.Infrastructure.Printing.Pdf.InvoiceQuestPdfBuilder.BuildPdfBytes(sale, shopSettings);

        public byte[] BuildRaw(Sale sale, PrintSettings shopSettings)
        {
            throw new NotSupportedException("SimpleTemplate does not support raw ESC/POS.");
        }

        private Paragraph Separator()
        {
            return new Paragraph(new Run(new string('-', 32)))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }
    }
}
using POS_BIGA.Infrastructure.Printing.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace POS_BIGA.Infrastructure.Printing.Templates
{
    /// <summary>
    /// قالب طباعة التقارير على الطابعة الحرارية بأسلوب SimpleTemplate
    /// </summary>
    public class ReportSimpleTemplate
    {
        public FlowDocument Build(ReportData data, Settings.PrintSettings settings)
        {
            double receiptWidthMm = settings?.ReceiptWidthMm > 0 ? settings.ReceiptWidthMm : 80;
            double pageWidth      = receiptWidthMm * (96.0 / 25.4);
            int    fontSize       = settings?.ThermalFontSize > 0 ? settings.ThermalFontSize : 11;
            bool   showLines      = settings?.ShowThermalLines ?? true;
            string currency       = settings?.CurrencySymbol ?? "د.م";
            int    dp             = settings?.DecimalPlaces ?? 2;

            // ─── مفاتيح الأقسام ────────────────────────────────────────
            bool secHeader       = settings?.ReportShowHeader      ?? true;
            bool secSummary      = settings?.ReportShowSummary     ?? true;
            bool secTopProducts  = settings?.ReportShowTopProducts  ?? true;
            bool secCategories   = settings?.ReportShowCategories   ?? true;
            bool secDailySales   = settings?.ReportShowDailySales   ?? true;
            bool secCredits      = settings?.ReportShowCredits      ?? true;
            bool secCreditsList  = settings?.ReportShowCreditsList  ?? false;

            var doc = new FlowDocument
            {
                FontFamily    = new FontFamily("Courier New"),
                FontSize      = fontSize,
                PagePadding   = new Thickness(6),
                PageWidth     = pageWidth,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.ColumnWidth = pageWidth - doc.PagePadding.Left - doc.PagePadding.Right;

            // ══════════════════════ رأس التقرير ══════════════════════
            if (secHeader)
            {
                if (!string.IsNullOrWhiteSpace(settings?.ShopName))
                    doc.Blocks.Add(CenterBold(settings.ShopName, fontSize + 3));
                if (!string.IsNullOrWhiteSpace(settings?.ShopAddress))
                    doc.Blocks.Add(Center(settings.ShopAddress, fontSize - 1));
                if (!string.IsNullOrWhiteSpace(settings?.ShopPhone))
                    doc.Blocks.Add(Center("Tel: " + settings.ShopPhone, fontSize - 1));
            }

            doc.Blocks.Add(CenterBold("تقرير المبيعات", fontSize + 1));
            doc.Blocks.Add(Center($"من: {data.StartDate:dd/MM/yyyy}  إلى: {data.EndDate:dd/MM/yyyy}", fontSize - 1));
            doc.Blocks.Add(Center($"طُبع: {DateTime.Now:dd/MM/yyyy HH:mm}", fontSize - 1));
            if (showLines) doc.Blocks.Add(Separator());

            // ══════════════════════ ملخص الأداء ══════════════════════
            if (secSummary)
            {
                doc.Blocks.Add(CenterBold("*** ملخص الأداء ***", fontSize));
                AddLine(doc, "إجمالي المبيعات", $"{data.TotalSales.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "إجمالي الأرباح",  $"{data.TotalProfit.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "عدد الفواتير",    data.InvoicesCount.ToString());
                AddLine(doc, "معدل الفاتورة",   $"{data.AverageSale.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "هامش الربح",      $"{data.ProfitMarginPercent:N1}%");
                AddLine(doc, "نمو المبيعات",    data.SalesGrowthFormatted);
            }

            // ══════════════════════ ملخص الديون ══════════════════════
            if (secCredits && data.TotalCredits > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** ملخص الديون ***", fontSize));
                AddLine(doc, "إجمالي الديون", $"{data.TotalCredits.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "المدفوع",        $"{data.PaidCredits.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "المتبقي",        $"{data.RemainingCredits.ToString($"N{dp}", CultureInfo.CurrentCulture)} {currency}");
                AddLine(doc, "نسبة التسديد",   $"{data.CreditPaymentRate:N1}%");
            }

            // ══════════════════════ قائمة زبائن الكريدي ══════════════
            if (secCreditsList && data.CreditCustomers != null && data.CreditCustomers.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** زبائن الكريدي ***", fontSize));

                var tbl = BuildTable(new[] { "الزبون", "الدين" }, new[] { 165, 110 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);

                foreach (var c in data.CreditCustomers.OrderByDescending(x => x.RemainingAmount))
                {
                    string name = c.CustomerName.Length > 18 ? c.CustomerName[..17] + "…" : c.CustomerName;
                    var row = new TableRow();
                    row.Cells.Add(Cell(name, TextAlignment.Right));
                    row.Cells.Add(Cell(c.RemainingAmount.ToString($"N{dp}", CultureInfo.CurrentCulture), TextAlignment.Left));
                    grp.Rows.Add(row);
                }
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ أفضل المنتجات ══════════════════════
            if (secTopProducts && data.TopProducts != null && data.TopProducts.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** أفضل المنتجات (Top 10) ***", fontSize));

                var tbl = BuildTable(new[] { "المنتج", "الكمية", "الإيرادات" }, new[] { 130, 55, 90 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);
                int rank = 1;
                decimal sumQtyT = 0, sumRevT = 0;
                foreach (var p in data.TopProducts.Take(10))
                {
                    string name = p.Name.Length > 16 ? p.Name[..15] + "…" : p.Name;
                    var row = new TableRow();
                    row.Cells.Add(Cell($"{rank++}. {name}",                                              TextAlignment.Right));
                    row.Cells.Add(Cell(p.QuantitySold.ToString($"N{dp}", CultureInfo.CurrentCulture),   TextAlignment.Center));
                    row.Cells.Add(Cell(p.TotalRevenue.ToString($"N{dp}", CultureInfo.CurrentCulture),   TextAlignment.Left));
                    grp.Rows.Add(row);
                    sumQtyT += p.QuantitySold;
                    sumRevT += p.TotalRevenue;
                }
                AddTotalRow(tbl, new[]
                {
                    "المجموع",
                    sumQtyT.ToString($"N{dp}", CultureInfo.CurrentCulture),
                    sumRevT.ToString($"N{dp}", CultureInfo.CurrentCulture)
                }, dp);
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ مبيعات الفئات ══════════════════════
            if (secCategories && data.CategorySales != null && data.CategorySales.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** مبيعات الفئات ***", fontSize));

                var tbl = BuildTable(new[] { "الفئة", "الإيرادات", "%" }, new[] { 140, 90, 45 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);
                decimal sumRevC = 0;
                foreach (var c in data.CategorySales.OrderByDescending(x => x.TotalRevenue))
                {
                    string name = c.CategoryName.Length > 16 ? c.CategoryName[..15] + "…" : c.CategoryName;
                    var row = new TableRow();
                    row.Cells.Add(Cell(name,                                                             TextAlignment.Right));
                    row.Cells.Add(Cell(c.TotalRevenue.ToString($"N{dp}", CultureInfo.CurrentCulture),   TextAlignment.Center));
                    row.Cells.Add(Cell($"{c.Percentage:N1}%",                                           TextAlignment.Left));
                    grp.Rows.Add(row);
                    sumRevC += c.TotalRevenue;
                }
                AddTotalRow(tbl, new[]
                {
                    "المجموع",
                    sumRevC.ToString($"N{dp}", CultureInfo.CurrentCulture),
                    "100%"
                }, dp);
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ المبيعات اليومية ══════════════════
            if (secDailySales && data.DailySales != null && data.DailySales.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** المبيعات اليومية ***", fontSize));

                var tbl = BuildTable(new[] { "التاريخ", "فواتير", "الإجمالي" }, new[] { 90, 50, 90 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);
                decimal sumSales = 0; int sumInv = 0;
                foreach (var d in data.DailySales.OrderByDescending(x => x.Date).Take(20))
                {
                    var row = new TableRow();
                    row.Cells.Add(Cell(d.Date.ToString("dd/MM/yyyy"),                                   TextAlignment.Right));
                    row.Cells.Add(Cell(d.InvoicesCount.ToString(),                                      TextAlignment.Center));
                    row.Cells.Add(Cell(d.TotalSales.ToString($"N{dp}", CultureInfo.CurrentCulture),     TextAlignment.Left));
                    grp.Rows.Add(row);
                    sumSales += d.TotalSales;
                    sumInv   += d.InvoicesCount;
                }
                AddTotalRow(tbl, new[]
                {
                    "المجموع",
                    sumInv.ToString(),
                    sumSales.ToString($"N{dp}", CultureInfo.CurrentCulture)
                }, dp);
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ المنتجات المباعة ══════════════════
            if ((settings?.ReportShowSoldProducts ?? false) && data.SoldProducts != null && data.SoldProducts.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** المنتجات المباعة ***", fontSize));

                var tbl = BuildTable(new[] { "المنتج", "الكمية", "الإجمالي", "الربح" }, new[] { 95, 45, 80, 55 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);

                decimal sumQty = 0, sumRev = 0, sumProfit = 0;
                foreach (var p in data.SoldProducts)
                {
                    string name = p.Name.Length > 11 ? p.Name[..10] + "…" : p.Name;
                    var row = new TableRow();
                    row.Cells.Add(Cell(name,                                                              TextAlignment.Right));
                    row.Cells.Add(Cell(p.QuantitySold.ToString($"N{dp}", CultureInfo.CurrentCulture),    TextAlignment.Center));
                    row.Cells.Add(Cell(p.TotalRevenue.ToString($"N{dp}", CultureInfo.CurrentCulture),    TextAlignment.Center));
                    row.Cells.Add(Cell(p.TotalProfit.ToString($"N{dp}", CultureInfo.CurrentCulture),     TextAlignment.Left));
                    grp.Rows.Add(row);
                    sumQty    += p.QuantitySold;
                    sumRev    += p.TotalRevenue;
                    sumProfit += p.TotalProfit;
                }
                AddTotalRow(tbl, new[]
                {
                    "المجموع",
                    sumQty.ToString($"N{dp}", CultureInfo.CurrentCulture),
                    sumRev.ToString($"N{dp}", CultureInfo.CurrentCulture),
                    sumProfit.ToString($"N{dp}", CultureInfo.CurrentCulture)
                }, dp);
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ تفاصيل زبائن الكريدي ══════════════
            if ((settings?.ReportShowCreditDetails ?? false) && data.CreditDetails != null && data.CreditDetails.Count > 0)
            {
                if (showLines) doc.Blocks.Add(Separator());
                doc.Blocks.Add(CenterBold("*** تفاصيل زبائن الكريدي ***", fontSize));

                var tbl = BuildTable(new[] { "الزبون", "المتبقي", "التاريخ" }, new[] { 120, 75, 80 });
                var grp = new TableRowGroup();
                tbl.RowGroups.Add(grp);
                foreach (var c in data.CreditDetails.OrderByDescending(x => x.RemainingAmount))
                {
                    string name = c.CustomerName.Length > 13 ? c.CustomerName[..12] + "…" : c.CustomerName;
                    var row = new TableRow();
                    row.Cells.Add(Cell(name, TextAlignment.Right));
                    row.Cells.Add(Cell(c.RemainingAmount.ToString($"N{dp}", CultureInfo.CurrentCulture), TextAlignment.Center));
                    row.Cells.Add(Cell(c.CreatedDate.ToString("dd/MM/yy"), TextAlignment.Left));
                    grp.Rows.Add(row);
                }
                doc.Blocks.Add(tbl);
            }

            // ══════════════════════ تذييل ══════════════════════
            if (showLines) doc.Blocks.Add(Separator());
            doc.Blocks.Add(Center(settings?.ShopName ?? "BIGA POS", fontSize - 1));
            doc.Blocks.Add(Center("شكراً لثقتكم", fontSize - 1));

            return doc;
        }

        // ─── helpers ───────────────────────────────────────────────────

        private static Paragraph CenterBold(string text, int size) =>
            new Paragraph(new Run(text))
            {
                FontWeight    = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                FontSize      = size,
                Margin        = new Thickness(0, 2, 0, 2)
            };

        private static Paragraph Center(string text, int size) =>
            new Paragraph(new Run(text))
            {
                TextAlignment = TextAlignment.Center,
                FontSize      = size,
                Margin        = new Thickness(0, 1, 0, 1)
            };

        private static Paragraph Separator() =>
            new Paragraph(new Run(new string('-', 32)))
            {
                TextAlignment = TextAlignment.Center,
                Margin        = new Thickness(0, 3, 0, 3)
            };

        private static void AddLine(FlowDocument doc, string label, string value)
        {
            var p = new Paragraph { TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 1, 0, 1) };
            p.Inlines.Add(new Run($"{label}: "));
            p.Inlines.Add(new Run(value) { FontWeight = FontWeights.Bold });
            doc.Blocks.Add(p);
        }

        private static Table BuildTable(string[] headers, int[] widths)
        {
            var tbl = new Table { CellSpacing = 0, BorderThickness = new Thickness(0), FontSize = 9 };
            for (int i = 0; i < widths.Length; i++)
                tbl.Columns.Add(new TableColumn { Width = new GridLength(widths[i]) });

            var hGroup = new TableRowGroup();
            var hRow   = new TableRow { FontWeight = FontWeights.Bold };
            for (int i = 0; i < headers.Length; i++)
            {
                var align = i == 0 ? TextAlignment.Right
                          : i == headers.Length - 1 ? TextAlignment.Left
                          : TextAlignment.Center;
                hRow.Cells.Add(new TableCell(new Paragraph(new Run(headers[i]))
                    { Margin = new Thickness(0), TextAlignment = align, FontSize = 9 })
                    { Padding = new Thickness(2), BorderThickness = new Thickness(0, 0, 0, 1), BorderBrush = Brushes.Gray });
            }
            hGroup.Rows.Add(hRow);
            tbl.RowGroups.Add(hGroup);
            return tbl;
        }

        private static void AddTotalRow(Table tbl, string[] values, int dp)
        {
            var footGroup = new TableRowGroup();
            var footRow   = new TableRow { FontWeight = FontWeights.Bold };
            int cols      = tbl.Columns.Count;
            for (int i = 0; i < cols; i++)
            {
                var align = i == 0 ? TextAlignment.Right
                          : i == cols - 1 ? TextAlignment.Left
                          : TextAlignment.Center;
                var cell = new TableCell(new Paragraph(new Run(i < values.Length ? values[i] : ""))
                    { Margin = new Thickness(0), TextAlignment = align, FontSize = 9 })
                    { Padding = new Thickness(2), BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = Brushes.Gray };
                footRow.Cells.Add(cell);
            }
            footGroup.Rows.Add(footRow);
            tbl.RowGroups.Add(footGroup);
        }

        private static TableCell Cell(string text, TextAlignment align) =>
            new TableCell(new Paragraph(new Run(text))
                { Margin = new Thickness(0), TextAlignment = align })
                { Padding = new Thickness(2) };
    }

    // ─── DTO بيانات التقرير ────────────────────────────────────────────

    public class ReportData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate   { get; set; }

        public decimal TotalSales          { get; set; }
        public decimal TotalProfit         { get; set; }
        public int     InvoicesCount       { get; set; }
        public decimal AverageSale         { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public decimal SalesGrowth         { get; set; }
        public string  SalesGrowthFormatted => SalesGrowth >= 0 ? $"+{SalesGrowth:N1}%" : $"{SalesGrowth:N1}%";

        public decimal TotalCredits      { get; set; }
        public decimal PaidCredits       { get; set; }
        public decimal RemainingCredits  { get; set; }
        public decimal CreditPaymentRate { get; set; }

        public List<ReportTopProduct>    TopProducts    { get; set; } = new();
        public List<ReportCategorySales> CategorySales  { get; set; } = new();
        public List<ReportDailySales>    DailySales     { get; set; } = new();
        public List<ReportCreditCustomer> CreditCustomers { get; set; } = new();
        public List<ReportSoldProduct>    SoldProducts    { get; set; } = new();
        public List<ReportCreditDetail>   CreditDetails   { get; set; } = new();
    }

    public class ReportCreditCustomer
    {
        public string  CustomerName    { get; set; } = "";
        public decimal RemainingAmount { get; set; }
    }

    public class ReportTopProduct
    {
        public string  Name          { get; set; } = "";
        public decimal QuantitySold  { get; set; }
        public decimal TotalRevenue  { get; set; }
    }

    public class ReportCategorySales
    {
        public string  CategoryName  { get; set; } = "";
        public decimal TotalRevenue  { get; set; }
        public decimal Percentage    { get; set; }
    }

    public class ReportDailySales
    {
        public DateTime Date          { get; set; }
        public int      InvoicesCount { get; set; }
        public decimal  TotalSales    { get; set; }
    }

    public class ReportSoldProduct
    {
        public string  Name         { get; set; } = "";
        public decimal QuantitySold { get; set; }
        public decimal UnitPrice    { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost    { get; set; }
        public decimal TotalProfit  => TotalRevenue - TotalCost;
    }

    public class ReportCreditDetail
    {
        public string  CustomerName    { get; set; } = "";
        public string  SaleNumber      { get; set; } = "";
        public decimal OriginalAmount  { get; set; }
        public decimal PaidAmount      { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime CreatedDate   { get; set; }
    }

    public class CreditCustomer
    {
        public string  CustomerName    { get; set; } = "";
        public decimal TotalDebt       { get; set; }
        public decimal PaidAmount      { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}

using ClosedXML.Excel;
using POS_BIGA.Infrastructure.Printing.Templates;
using POS_BIGA.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace POS_BIGA.Infrastructure.Excel
{
    /// <summary>
    /// 📊 تصدير تقارير المبيعات/الديون إلى ملف Excel (.xlsx) حقيقي عبر ClosedXML.
    /// يحل محل التصدير القديم الذي كان ينتج ملف CSV نصياً باسم مضلِّل.
    /// </summary>
    public static class ReportExcelExporter
    {
        private const string HeaderFill = "#0D47A1";
        private const string HeaderFont = "#FFFFFF";
        private const string AltRowFill = "#F5F7FA";

        // ════════════════════════════════════════════════════════════════════
        // تقرير شامل (الملخص + الفواتير + أفضل المنتجات + الفئات + الديون)
        // ════════════════════════════════════════════════════════════════════

        public static void SaveFullReport(ReportData data, List<SaleReportDto> sales, List<CreditReportDto> credits, string filePath)
        {
            using var wb = new XLWorkbook();

            AddSummarySheet(wb, data);
            AddSalesSheet(wb, sales ?? new List<SaleReportDto>());

            if (data?.TopProducts?.Count > 0)
                AddTopProductsSheet(wb, data.TopProducts);

            if (data?.CategorySales?.Count > 0)
                AddCategorySheet(wb, data.CategorySales);

            if (credits?.Count > 0)
                AddCreditsSheet(wb, credits);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            wb.SaveAs(filePath);
        }

        /// <summary>تصدير ورقة ديون مستقلة فقط (لتبويب الديون).</summary>
        public static void SaveCreditsOnly(List<CreditReportDto> credits, string filePath)
        {
            using var wb = new XLWorkbook();
            AddCreditsSheet(wb, credits ?? new List<CreditReportDto>());
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            wb.SaveAs(filePath);
        }

        // ─── أوراق العمل ───────────────────────────────────────────────────────

        private static void AddSummarySheet(XLWorkbook wb, ReportData data)
        {
            var ws = wb.Worksheets.Add("الملخص");
            ws.RightToLeft = true;

            ws.Cell(1, 1).Value = "تقرير المبيعات الشامل";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Range(1, 1, 1, 2).Merge();

            ws.Cell(2, 1).Value = "الفترة";
            ws.Cell(2, 2).Value = data == null ? string.Empty : $"{data.StartDate:yyyy-MM-dd} إلى {data.EndDate:yyyy-MM-dd}";

            var rows = new (string Label, object Value, string Format)[]
            {
                ("إجمالي المبيعات", data?.TotalSales ?? 0m, "#,##0.00"),
                ("إجمالي الأرباح", data?.TotalProfit ?? 0m, "#,##0.00"),
                ("هامش الربح (%)", data?.ProfitMarginPercent ?? 0m, "0.0\"%\""),
                ("عدد الفواتير", data?.InvoicesCount ?? 0, "#,##0"),
                ("معدل الفاتورة", data?.AverageSale ?? 0m, "#,##0.00"),
                ("نمو المبيعات (%)", data?.SalesGrowth ?? 0m, "0.0\"%\""),
                ("إجمالي الديون", data?.TotalCredits ?? 0m, "#,##0.00"),
                ("الديون المدفوعة", data?.PaidCredits ?? 0m, "#,##0.00"),
                ("الديون المتبقية", data?.RemainingCredits ?? 0m, "#,##0.00"),
                ("نسبة تسديد الديون (%)", data?.CreditPaymentRate ?? 0m, "0.0\"%\""),
            };

            int r = 4;
            ws.Cell(r, 1).Value = "المؤشر";
            ws.Cell(r, 2).Value = "القيمة";
            StyleHeaderRow(ws.Range(r, 1, r, 2));
            r++;

            foreach (var (label, value, format) in rows)
            {
                ws.Cell(r, 1).Value = label;
                var cell = ws.Cell(r, 2);
                if (value is decimal dec) cell.Value = dec;
                else if (value is int i) cell.Value = i;
                cell.Style.NumberFormat.Format = format;

                if (r % 2 == 0)
                    ws.Range(r, 1, r, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowFill);

                r++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(4);
        }

        private static void AddSalesSheet(XLWorkbook wb, List<SaleReportDto> sales)
        {
            var ws = wb.Worksheets.Add("الفواتير");
            ws.RightToLeft = true;

            var headers = new[] { "رقم الفاتورة", "التاريخ", "الزبون", "عدد الأصناف", "طريقة الدفع", "الإجمالي", "المدفوع", "المتبقي", "الربح", "هامش الربح" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws.Range(1, 1, 1, headers.Length));

            int row = 2;
            foreach (var s in sales.OrderByDescending(x => x.SaleDate))
            {
                ws.Cell(row, 1).Value = s.SaleNumber ?? string.Empty;
                ws.Cell(row, 2).Value = s.SaleDate;
                ws.Cell(row, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                ws.Cell(row, 3).Value = s.CustomerName ?? string.Empty;
                ws.Cell(row, 4).Value = s.ItemsCount;
                ws.Cell(row, 5).Value = s.PaymentMethod ?? string.Empty;
                ws.Cell(row, 6).Value = s.TotalAmount;
                ws.Cell(row, 7).Value = s.PaidAmount;
                ws.Cell(row, 8).Value = s.RemainingAmount;
                ws.Cell(row, 9).Value = s.TotalProfit;
                ws.Cell(row, 10).Value = s.ProfitMarginFormatted ?? string.Empty;

                foreach (var col in new[] { 6, 7, 8, 9 })
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowFill);

                row++;
            }

            if (sales.Count > 0)
            {
                ws.Cell(row, 5).Value = "الإجمالي";
                ws.Cell(row, 5).Style.Font.Bold = true;
                ws.Cell(row, 6).Value = sales.Sum(x => x.TotalAmount);
                ws.Cell(row, 7).Value = sales.Sum(x => x.PaidAmount);
                ws.Cell(row, 8).Value = sales.Sum(x => x.RemainingAmount);
                ws.Cell(row, 9).Value = sales.Sum(x => x.TotalProfit);
                foreach (var col in new[] { 6, 7, 8, 9 })
                {
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, col).Style.Font.Bold = true;
                }
                ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        private static void AddTopProductsSheet(XLWorkbook wb, List<ReportTopProduct> products)
        {
            var ws = wb.Worksheets.Add("أفضل المنتجات");
            ws.RightToLeft = true;

            var headers = new[] { "#", "المنتج", "الكمية المباعة", "الإيرادات" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws.Range(1, 1, 1, headers.Length));

            int row = 2, rank = 1;
            foreach (var p in products)
            {
                ws.Cell(row, 1).Value = rank;
                ws.Cell(row, 2).Value = p.Name ?? string.Empty;
                ws.Cell(row, 3).Value = p.QuantitySold;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = p.TotalRevenue;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowFill);

                row++; rank++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        private static void AddCategorySheet(XLWorkbook wb, List<ReportCategorySales> categories)
        {
            var ws = wb.Worksheets.Add("الفئات");
            ws.RightToLeft = true;

            var headers = new[] { "الفئة", "الإيرادات", "النسبة (%)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws.Range(1, 1, 1, headers.Length));

            int row = 2;
            foreach (var c in categories.OrderByDescending(x => x.TotalRevenue))
            {
                ws.Cell(row, 1).Value = c.CategoryName ?? string.Empty;
                ws.Cell(row, 2).Value = c.TotalRevenue;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 3).Value = c.Percentage;
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.0";

                if (row % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowFill);

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        private static void AddCreditsSheet(XLWorkbook wb, List<CreditReportDto> credits)
        {
            var ws = wb.Worksheets.Add("الديون");
            ws.RightToLeft = true;

            var headers = new[] { "الزبون", "رقم الفاتورة", "تاريخ الإنشاء", "تاريخ الاستحقاق", "المبلغ الأصلي", "المدفوع", "المتبقي", "الحالة", "نسبة السداد (%)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];
            StyleHeaderRow(ws.Range(1, 1, 1, headers.Length));

            int row = 2;
            foreach (var c in credits.OrderByDescending(x => x.CreatedDate))
            {
                ws.Cell(row, 1).Value = c.CustomerName ?? string.Empty;
                ws.Cell(row, 2).Value = c.SaleNumber ?? string.Empty;
                ws.Cell(row, 3).Value = c.CreatedDate;
                ws.Cell(row, 3).Style.DateFormat.Format = "yyyy-mm-dd";
                ws.Cell(row, 4).Value = c.DueDateFormatted ?? string.Empty;
                ws.Cell(row, 5).Value = c.OriginalAmount;
                ws.Cell(row, 6).Value = c.PaidAmount;
                ws.Cell(row, 7).Value = c.RemainingAmount;
                ws.Cell(row, 8).Value = c.Status ?? string.Empty;
                ws.Cell(row, 9).Value = c.PaymentPercentage;

                foreach (var col in new[] { 5, 6, 7 })
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 9).Style.NumberFormat.Format = "0.0";

                // تلوين حالة الدين
                var statusCell = ws.Cell(row, 8);
                statusCell.Style.Font.Bold = true;
                if (!string.IsNullOrEmpty(c.StatusColor))
                {
                    try { statusCell.Style.Font.FontColor = XLColor.FromHtml(c.StatusColor); } catch { }
                }

                if (row % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowFill);

                row++;
            }

            if (credits.Count > 0)
            {
                ws.Cell(row, 4).Value = "الإجمالي";
                ws.Cell(row, 4).Style.Font.Bold = true;
                ws.Cell(row, 5).Value = credits.Sum(x => x.OriginalAmount);
                ws.Cell(row, 6).Value = credits.Sum(x => x.PaidAmount);
                ws.Cell(row, 7).Value = credits.Sum(x => x.RemainingAmount);
                foreach (var col in new[] { 5, 6, 7 })
                {
                    ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell(row, col).Style.Font.Bold = true;
                }
                ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Medium;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        // ─── مساعد التنسيق ─────────────────────────────────────────────────────

        private static void StyleHeaderRow(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.FromHtml(HeaderFont);
            range.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }
}

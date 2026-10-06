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
using System.Windows.Controls;

namespace POS_BIGA.Infrastructure.Printing.Templates
{
    /// <summary>
    /// 📄 قالب جديد كلياً (Modern & Clean) لتوصيلات A4/A5 يحل جميع مشاكل ارتفاع الأسطر Layout
    /// </summary>
    public class CompactTemplate : IPrintTemplate
    {
        public string Name => "Professional A4/A5";

        // ألوان عصرية ونظيفة
        private static readonly Brush ColorPrimary = new SolidColorBrush(Color.FromRgb(15, 23, 42));    // أسود/كحلي داكن (Slate 900)
        private static readonly Brush ColorSecondary = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // رمادي للتواريخ (Slate 500)
        private static readonly Brush ColorAccent = new SolidColorBrush(Color.FromRgb(2, 132, 199));   // أزرق سماوي للعناوين (Sky 600)
        private static readonly Brush ColorLightBg = new SolidColorBrush(Color.FromRgb(248, 250, 252)); // خلفية خفيفة (Slate 50)
        private static readonly Brush ColorBorder = new SolidColorBrush(Color.FromRgb(226, 232, 240));  // فواصل ناعمة (Slate 200)

        public FlowDocument BuildPreview(Sale sale, PrintSettings s)
        {
            if (sale == null)
                return new FlowDocument(new Paragraph(new Run("لا توجد بيانات للفاتورة.")));

            sale.TaxRate = s?.TaxRate ?? 0m;
            sale.CalculateTotals();

            int dp = s?.DecimalPlaces ?? 2;
            string cur = s?.Currency ?? "MAD";
            bool isA5 = s?.PaperSize == PaperSize.A5;

            // ضبط المقاسات حسب الورق
            double pw = isA5 ? 559.0 : 816.0;
            double ph = isA5 ? 794.0 : 1056.0;
            double pad = isA5 ? 20.0 : 40.0;
            int baseFontSize = isA5 ? 10 : 12;

            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI, Arial, sans-serif"),
                FontSize = baseFontSize,
                PagePadding = new Thickness(pad),
                PageWidth = pw,
                PageHeight = ph,
                ColumnWidth = pw - (pad * 2),
                Background = Brushes.White,
                Foreground = ColorPrimary,
                FlowDirection = FlowDirection.LeftToRight
            };

            // =======================================================
            // 1. HEADER (Shop Info + Document Title)
            // =======================================================

            // 1. تهيئة الـ Grid والأعمدة دفعة واحدة
            var headerGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 20),
                ColumnDefinitions =
    {
        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
    }
            };

            // ==========================================
            // Shop Info (Left)
            // ==========================================
            var shopStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };

            // إضافة اسم الشركة
            shopStack.Children.Add(new TextBlock
            {
                Text = (s?.ShopName ?? "اسم الشركة").ToUpper(),
                FontSize = isA5 ? 16 : 20,
                FontWeight = FontWeights.Bold,
                Foreground = ColorPrimary,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // إضافة الهاتف إن وجد
            if (!string.IsNullOrWhiteSpace(s?.ShopPhone))
            {
                shopStack.Children.Add(new TextBlock
                {
                    Text = $"Tél: {s.ShopPhone}",
                    FontSize = isA5 ? 10 : 11,
                    Foreground = ColorSecondary
                });
            }

            // إضافة العنوان إن وجد
            if (!string.IsNullOrWhiteSpace(s?.ShopAddress))
            {
                shopStack.Children.Add(new TextBlock
                {
                    Text = s.ShopAddress,
                    FontSize = isA5 ? 10 : 11,
                    Foreground = ColorSecondary,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 250
                });
            }

            Grid.SetColumn(shopStack, 0);
            headerGrid.Children.Add(shopStack);

            // ==========================================
            // Document Title (Right)
            // ==========================================

            // 2. استخراج المتغيرات المنطقية لتنظيف كود الواجهة
            string saleRef = sale.SaleNumber ?? sale.Id.ToString().PadLeft(5, '0');
            DateTime saleDate = sale.SaleDate != default ? sale.SaleDate : DateTime.Now;

            // 3. استخدام Collection Initializer لإضافة العناصر مباشرة
            var titleStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right,
                Children =
    {
        new TextBlock
        {
            Text = "BON DE LIVRAISON",
            FontSize = isA5 ? 18 : 24,
            FontWeight = FontWeights.ExtraBold,
            Foreground = ColorAccent,
            TextAlignment = TextAlignment.Right
        },
        new TextBlock
        {
            Text = $"Référence: {saleRef}",
            FontSize = isA5 ? 11 : 13,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 5, 0, 0)
        },
        new TextBlock
        {
            Text = $"Date: {saleDate:dd/MM/yyyy HH:mm}",
            FontSize = isA5 ? 10 : 11,
            Foreground = ColorSecondary,
            TextAlignment = TextAlignment.Right
        }
    }
            };

            Grid.SetColumn(titleStack, 1);
            headerGrid.Children.Add(titleStack);

            // إضافة الـ Grid النهائي إلى المستند
            doc.Blocks.Add(new BlockUIContainer(headerGrid));

            // =======================================================
            // 3. PRODUCTS TABLE (UI Grid rows + Smart Pagination)
            // =======================================================
            var itemsList = sale.Items?.ToList() ?? new System.Collections.Generic.List<SaleItem>();

            Border BuildRow(string c1, string c2, string c3, string c4, Brush bg, bool isHeader = false)
            {
                var row = new Grid
                {
                    Background = bg,
                    Height = isHeader ? (isA5 ? 36 : 40) : (isA5 ? 28 : 32)
                };

                // بدون عمود Référence - مساحات متناسقة
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(isA5 ? 75 : 95) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(isA5 ? 90 : 120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(isA5 ? 100 : 130) });

                // الفواصل العمودية تُرسَم أولاً لتكون في الخلفية
                for (int i = 1; i < 4; i++)
                {
                    var v = new Border { Width = 1, Background = ColorBorder, HorizontalAlignment = HorizontalAlignment.Left };
                    Grid.SetColumn(v, i);
                    row.Children.Add(v);
                }

                // دالة الخلية الجديدة تعتمد على محاذاة العنصر نفسه وليس النص فقط
                Border Cell(string txt, int col, HorizontalAlignment align)
                {
                    var tb = new TextBlock
                    {
                        Text = txt ?? string.Empty,
                        FontSize = isHeader ? (isA5 ? 10 : 12) : (isA5 ? 9 : 11),
                        FontWeight = isHeader ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = isHeader ? Brushes.White : (col == 0 ? ColorPrimary : ColorSecondary), // لون مميز للأسماء
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = align,
                        Padding = isHeader ? new Thickness(0, 1, 0, 0) : new Thickness(0) // إزاحة بسيطة للأسفل لرأس الجدول لتفادي الاحتكاك بالخط العلوي
                    };

                    // تغليف النص بـ Border لتأمين مساحة الحواف وتوسيطه بدقة صارمة
                    var container = new Border
                    {
                        Padding = new Thickness(10, 0, 10, 0),
                        Child = tb,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    
                    Grid.SetColumn(container, col);
                    return container;
                }

                row.Children.Add(Cell(c1, 0, HorizontalAlignment.Left));
                row.Children.Add(Cell(c2, 1, HorizontalAlignment.Center));
                row.Children.Add(Cell(c3, 2, HorizontalAlignment.Right));
                row.Children.Add(Cell(c4, 3, HorizontalAlignment.Right));

                return new Border
                {
                    BorderBrush = ColorBorder,
                    BorderThickness = new Thickness(1, 0, 1, 1),
                    Child = row
                };
            }

            BlockUIContainer BuildItemsPage(System.Collections.Generic.IEnumerable<SaleItem> pageItems, bool isFirstPage)
            {
                var itemsPanel = new StackPanel { Margin = new Thickness(0, isFirstPage ? 0 : 4, 0, 15) };

                var headerRow = BuildRow("Désignation", "Qte", "P.U", "Total", ColorPrimary, isHeader: true);
                headerRow.BorderThickness = new Thickness(1, 1, 1, 1);
                headerRow.CornerRadius = new CornerRadius(6, 6, 0, 0); // زوايا دائرية للهيدر دائماً
                itemsPanel.Children.Add(headerRow);

                int rnLocal = 0;
                var pageItemsList = pageItems.ToList();
                int pageCount = pageItemsList.Count;

                foreach (var item in pageItemsList)
                {
                    rnLocal++;
                    var bg = rnLocal % 2 == 0 ? ColorLightBg : Brushes.White;

                    string qTxt = (item.Quantity == Math.Floor(item.Quantity))
                        ? item.Quantity.ToString("0")
                        : item.Quantity.ToString($"N{dp}", CultureInfo.InvariantCulture);

                    string pTxt = item.UnitPrice.ToString($"N{dp}", CultureInfo.InvariantCulture);
                    string tTxt = item.LineTotal.ToString($"N{dp}", CultureInfo.InvariantCulture);
                    string dTxt = item.Name ?? string.Empty;

                    var rowBorder = BuildRow(dTxt, qTxt, pTxt, tTxt, bg);
                    
                    // إغلاق الزوايا السفلية لآخر صف في كل صفحة
                    if (rnLocal == pageCount) rowBorder.CornerRadius = new CornerRadius(0, 0, 6, 6);
                    
                    itemsPanel.Children.Add(rowBorder);
                }

                return new BlockUIContainer(itemsPanel);
            }

            // =======================================================
            // تقسيم ذكي للمنتجات على صفحات لتفادي الفراغات المزعجة
            // =======================================================
            var chunks = new System.Collections.Generic.List<System.Collections.Generic.List<SaleItem>>();
            int totalItems = itemsList.Count;

            int singlePageCap = isA5 ? 8 : 14;   // سعة صفحة واحدة كاملة (بها الهيدر والفوتر)
            int firstCap      = isA5 ? 12 : 20;  // سعة الصفحة الأولى (هيدر فقط)
            int middleCap     = isA5 ? 18 : 28;  // سعة صفحة وسطى (بدون هيدر ولا فوتر)
            int lastCap       = isA5 ? 9 : 16;   // سعة الصفحة الأخيرة (بها مجاميع وفوتر)

            if (totalItems <= singlePageCap)
            {
                chunks.Add(itemsList); // كل شيء في صفحة واحدة
            }
            else if (totalItems <= firstCap)
            {
                // تكفي للصفحة الأولى لكنها تเบز عن مساحة الفوتر، فنقسمها نصفين متوازنين
                int half = totalItems / 2 + 1;
                chunks.Add(itemsList.GetRange(0, half));
                chunks.Add(itemsList.GetRange(half, totalItems - half));
            }
            else
            {
                // أكثر من صفحة، نبدأ بالصفحة الأولى
                chunks.Add(itemsList.GetRange(0, firstCap));
                int index = firstCap;
                int remaining = totalItems - firstCap;

                while (remaining > 0)
                {
                    if (remaining <= lastCap)
                    {
                        // المتبقي يكفي للصفحة الأخيرة مع الفوتر
                        chunks.Add(itemsList.GetRange(index, remaining));
                        break;
                    }
                    else if (remaining <= middleCap)
                    {
                        // المتبقي يكفي لصفحة، لكن لا يوجد مكان للفوتر معها..
                        // إذن نقسم المتبقي على قسمين متوازينين للصفحة الحالية والصفحة التي تليها
                        int take = remaining - (lastCap - 2); 
                        if (take <= 0 || take >= remaining) take = remaining / 2;
                        
                        chunks.Add(itemsList.GetRange(index, take));
                        index += take;
                        remaining -= take;
                    }
                    else
                    {
                        // المتبقي أكبر من الصفحة الوسطى، نأخذ صفحة كاملة
                        int take = middleCap;
                        // لمنع ترك عنصر واحد أو اثنين فقط للصفحة الأخيرة
                        if (remaining - middleCap < 4)
                        {
                            take = middleCap - 4;
                        }
                        chunks.Add(itemsList.GetRange(index, take));
                        index += take;
                        remaining -= take;
                    }
                }
            }

            for (int i = 0; i < chunks.Count; i++)
            {
                var pageBlock = BuildItemsPage(chunks[i], isFirstPage: i == 0);
                pageBlock.BreakPageBefore = i > 0;
                doc.Blocks.Add(pageBlock);
            }

            // =======================================================
            // 4. TOTALS SECTION
            // =======================================================
            var totalsGrid = new Grid { Margin = new Thickness(0, 10, 0, 20) };
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer / Left info
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Totals Box

            // Left info (Items count)
            var statsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };
            int totQty = sale.Items?.Sum(i => (int)i.Quantity) ?? 0;
            statsStack.Children.Add(new TextBlock { Text = $"Total Articles: {sale.Items?.Count ?? 0}", FontSize = isA5 ? 10 : 11, Foreground = ColorSecondary });
            statsStack.Children.Add(new TextBlock { Text = $"Quantité Globale: {totQty}", FontSize = isA5 ? 10 : 11, Foreground = ColorSecondary });
            Grid.SetColumn(statsStack, 0);
            totalsGrid.Children.Add(statsStack);

            // Right Box
            var rightBorder = new Border
            {
                Background = ColorLightBg,
                BorderBrush = ColorBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15, 10, 15, 10),
                MinWidth = isA5 ? 180 : 250
            };
            var rightStack = new StackPanel();

            string ttTxt = sale.Total.ToString($"N{dp}");
            string pdTxt = sale.AmountPaid > 0 ? sale.AmountPaid.ToString($"N{dp}") : "0.00";
            
            // Subtotal
            var subGrid = new Grid { Margin = new Thickness(0,0,0,8) };
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var lblTotal = new TextBlock { Text = "TOTAL NET", FontSize = isA5 ? 12 : 14, FontWeight = FontWeights.Bold, Foreground = ColorPrimary };
            var valTotal = new TextBlock { Text = $"{ttTxt} {cur}", FontSize = isA5 ? 14 : 16, FontWeight = FontWeights.ExtraBold, Foreground = ColorAccent, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(lblTotal, 0); Grid.SetColumn(valTotal, 1);
            subGrid.Children.Add(lblTotal); subGrid.Children.Add(valTotal);
            rightStack.Children.Add(subGrid);

            // Separator
            rightStack.Children.Add(new Border { Height = 1, Background = ColorBorder, Margin = new Thickness(0,0,0,8) });

            // Paid & Rest
            var pGrid = new Grid { Margin = new Thickness(0,0,0,4) };
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var lblPaid = new TextBlock { Text = "Payé :", FontSize = isA5 ? 10 : 11, Foreground = ColorSecondary };
            var valPaid = new TextBlock { Text = $"{pdTxt} {cur}", FontSize = isA5 ? 10 : 11, FontWeight = FontWeights.SemiBold, Foreground = ColorPrimary, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(lblPaid, 0); Grid.SetColumn(valPaid, 1);
            pGrid.Children.Add(lblPaid); pGrid.Children.Add(valPaid);
            rightStack.Children.Add(pGrid);

            if (sale.AmountPaid > sale.Total)
            {
                var rGrid = new Grid();
                rGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var lblRendu = new TextBlock { Text = "Rendu :", FontSize = isA5 ? 10 : 11, Foreground = ColorSecondary };
                var valRendu = new TextBlock { Text = $"{(sale.AmountPaid - sale.Total).ToString($"N{dp}")} {cur}", FontSize = isA5 ? 10 : 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)), HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetColumn(lblRendu, 0); Grid.SetColumn(valRendu, 1);
                rGrid.Children.Add(lblRendu); rGrid.Children.Add(valRendu);
                rightStack.Children.Add(rGrid);
            }

            rightBorder.Child = rightStack;
            Grid.SetColumn(rightBorder, 1);
            totalsGrid.Children.Add(rightBorder);

            doc.Blocks.Add(new BlockUIContainer(totalsGrid));

            // =======================================================
            // 5. FOOTER
            // =======================================================
            var footerStack = new StackPanel { Margin = new Thickness(0, 30, 0, 0) };
            footerStack.Children.Add(new Border { Height = 1, Background = ColorBorder, Margin = new Thickness(0, 0, 0, 10) });

            var fGrid = new Grid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            fGrid.Children.Add(new TextBlock { Text = s?.InvoiceMessage ?? "Merci pour votre confiance !", FontSize = isA5 ? 9 : 10, Foreground = ColorSecondary, FontStyle = FontStyles.Italic });
            
            var sigTxt = new TextBlock { Text = "Cachet & Signature", FontSize = isA5 ? 10 : 11, FontWeight = FontWeights.Medium, Foreground = ColorPrimary, TextAlignment = TextAlignment.Right };
            Grid.SetColumn(sigTxt, 1);
            fGrid.Children.Add(sigTxt);

            footerStack.Children.Add(fGrid);
            doc.Blocks.Add(new BlockUIContainer(footerStack));

            return doc;
        }

        public byte[] BuildPdf(Sale sale, PrintSettings s)
            => POS_BIGA.Infrastructure.Printing.Pdf.InvoiceQuestPdfBuilder.BuildPdfBytes(sale, s);

        public byte[] BuildRaw(Sale sale, PrintSettings s)
            => throw new NotSupportedException("Professional A4/A5 uses FlowDocument only.");

        // ═══════════════════════════════════════════════════════════════
        // تقرير المبيعات A4 — يحترم سويتش الأقسام من الإعدادات
        // ═══════════════════════════════════════════════════════════════
        public FlowDocument BuildReportPreview(ReportData data, PrintSettings s)
        {
            bool isA5      = s?.PaperSize == PaperSize.A5;
            double pw      = isA5 ? 559.0 : 816.0;
            double ph      = isA5 ? 794.0 : 1056.0;
            double pad     = isA5 ? 20.0  : 36.0;
            int    fs      = isA5 ? 10    : 11;
            int    dp      = s?.DecimalPlaces ?? 2;
            string cur     = s?.CurrencySymbol ?? s?.Currency ?? "د.م";

            // ── سويتش الأقسام ──────────────────────────────────────
            bool secHeader      = s?.A4ReportShowHeader      ?? true;
            bool secSummary     = s?.A4ReportShowSummary     ?? true;
            bool secTopProducts = s?.A4ReportShowTopProducts ?? true;
            bool secCategories  = s?.A4ReportShowCategories  ?? true;
            bool secDailySales  = s?.A4ReportShowDailySales  ?? true;
            bool secCredits     = s?.A4ReportShowCredits     ?? true;
            bool secCreditsList = s?.A4ReportShowCreditsList ?? false;

            var doc = new FlowDocument
            {
                FontFamily  = new FontFamily("Segoe UI, Arial, sans-serif"),
                FontSize    = fs,
                PagePadding = new Thickness(pad),
                PageWidth   = pw,
                PageHeight  = ph,
                ColumnWidth = pw - pad * 2,
                Background  = Brushes.White,
                Foreground  = ColorPrimary,
                FlowDirection = FlowDirection.RightToLeft
            };

            // ── رأس الصفحة ─────────────────────────────────────────
            if (secHeader)
            {
                var hGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // يسار: بيانات المتجر
                var shopStack = new StackPanel { VerticalAlignment = VerticalAlignment.Top };
                if (!string.IsNullOrWhiteSpace(s?.ShopName))
                    shopStack.Children.Add(new TextBlock { Text = s.ShopName, FontSize = isA5 ? 16 : 20, FontWeight = FontWeights.Bold, Foreground = ColorPrimary });
                if (!string.IsNullOrWhiteSpace(s?.ShopAddress))
                    shopStack.Children.Add(new TextBlock { Text = s.ShopAddress, FontSize = fs - 1, Foreground = ColorSecondary, TextWrapping = TextWrapping.Wrap });
                if (!string.IsNullOrWhiteSpace(s?.ShopPhone))
                    shopStack.Children.Add(new TextBlock { Text = $"هاتف: {s.ShopPhone}", FontSize = fs - 1, Foreground = ColorSecondary });
                Grid.SetColumn(shopStack, 0);
                hGrid.Children.Add(shopStack);

                // يمين: عنوان التقرير
                var titleStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                titleStack.Children.Add(new TextBlock { Text = "تقرير المبيعات", FontSize = isA5 ? 18 : 22, FontWeight = FontWeights.ExtraBold, Foreground = ColorAccent, TextAlignment = TextAlignment.Right });
                titleStack.Children.Add(new TextBlock { Text = $"من: {data.StartDate:dd/MM/yyyy}   إلى: {data.EndDate:dd/MM/yyyy}", FontSize = fs, Foreground = ColorSecondary, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 4, 0, 0) });
                titleStack.Children.Add(new TextBlock { Text = $"طُبع: {DateTime.Now:dd/MM/yyyy HH:mm}", FontSize = fs - 1, Foreground = ColorSecondary, TextAlignment = TextAlignment.Right });
                Grid.SetColumn(titleStack, 1);
                hGrid.Children.Add(titleStack);

                doc.Blocks.Add(new BlockUIContainer(hGrid));
                doc.Blocks.Add(new BlockUIContainer(new Border { Height = 2, Background = ColorAccent, Margin = new Thickness(0, 0, 0, 16) }));
            }

            // ── ملخص الأداء ────────────────────────────────────────
            if (secSummary)
            {
                doc.Blocks.Add(SectionTitle("📊 ملخص الأداء", isA5));

                var summaryGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                for (int i = 0; i < 3; i++)
                    summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var stats = new[]
                {
                    ("إجمالي المبيعات",  $"{data.TotalSales.ToString($"N{dp}")} {cur}",     ColorAccent),
                    ("إجمالي الأرباح",   $"{data.TotalProfit.ToString($"N{dp}")} {cur}",    new SolidColorBrush(Color.FromRgb(22, 163, 74))),
                    ("عدد الفواتير",     data.InvoicesCount.ToString(),                       ColorPrimary),
                    ("معدل الفاتورة",    $"{data.AverageSale.ToString($"N{dp}")} {cur}",     ColorSecondary),
                    ("هامش الربح",       $"{data.ProfitMarginPercent:N1}%",                  ColorPrimary),
                    ("نمو المبيعات",     data.SalesGrowthFormatted,                           ColorPrimary),
                };

                for (int row = 0; row < 2; row++)
                {
                    summaryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    for (int col = 0; col < 3; col++)
                    {
                        int idx = row * 3 + col;
                        if (idx >= stats.Length) break;
                        var (label, value, color) = stats[idx];
                        var card = new Border
                        {
                            Margin = new Thickness(4),
                            Padding = new Thickness(10, 8, 10, 8),
                            Background = ColorLightBg,
                            BorderBrush = ColorBorder,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(6),
                            Child = new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock { Text = label, FontSize = fs - 1, Foreground = ColorSecondary },
                                    new TextBlock { Text = value, FontSize = isA5 ? 13 : 15, FontWeight = FontWeights.Bold, Foreground = color }
                                }
                            }
                        };
                        Grid.SetRow(card, row);
                        Grid.SetColumn(card, col);
                        summaryGrid.Children.Add(card);
                    }
                }
                doc.Blocks.Add(new BlockUIContainer(summaryGrid));
            }

            // ── ملخص الديون ────────────────────────────────────────
            if (secCredits && data.TotalCredits > 0)
            {
                doc.Blocks.Add(SectionTitle("💳 ملخص الديون", isA5));
                var t = BuildFlowTable(new[] { "إجمالي الديون", "المدفوع", "المتبقي", "نسبة التسديد" }, fs);
                var rg = new TableRowGroup();
                var tr = new TableRow { Background = ColorLightBg };
                tr.Cells.Add(TC($"{data.TotalCredits.ToString($"N{dp}")} {cur}", fs));
                tr.Cells.Add(TC($"{data.PaidCredits.ToString($"N{dp}")} {cur}", fs));
                tr.Cells.Add(TC($"{data.RemainingCredits.ToString($"N{dp}")} {cur}", fs));
                tr.Cells.Add(TC($"{data.CreditPaymentRate:N1}%", fs));
                rg.Rows.Add(tr);
                t.RowGroups.Add(rg);
                doc.Blocks.Add(t);
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });
            }

            // ── قائمة زبائن الدين ──────────────────────────────────
            if (secCreditsList && data.CreditCustomers?.Count > 0)
            {
                doc.Blocks.Add(SectionTitle("👥 زبائن الدين", isA5));
                var t = BuildFlowTable(new[] { "الزبون", "المتبقي" }, fs);
                var rg = new TableRowGroup();
                bool alt = false;
                foreach (var c in data.CreditCustomers.OrderByDescending(x => x.RemainingAmount))
                {
                    var tr = new TableRow { Background = alt ? ColorLightBg : Brushes.White };
                    tr.Cells.Add(TC(c.CustomerName, fs, TextAlignment.Right));
                    tr.Cells.Add(TC($"{c.RemainingAmount.ToString($"N{dp}")} {cur}", fs, TextAlignment.Left));
                    rg.Rows.Add(tr);
                    alt = !alt;
                }
                t.RowGroups.Add(rg);
                doc.Blocks.Add(t);
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });
            }

            // ── أفضل المنتجات ──────────────────────────────────────
            if (secTopProducts && data.TopProducts?.Count > 0)
            {
                doc.Blocks.Add(SectionTitle("🏆 أفضل 10 منتجات", isA5));
                var t = BuildFlowTable(new[] { "#", "المنتج", "الكمية", "الإيرادات" }, fs);
                var rg = new TableRowGroup();
                int rank = 1; bool alt = false;
                foreach (var p in data.TopProducts.Take(10))
                {
                    var tr = new TableRow { Background = alt ? ColorLightBg : Brushes.White };
                    tr.Cells.Add(TC(rank.ToString(), fs, TextAlignment.Center));
                    tr.Cells.Add(TC(p.Name, fs, TextAlignment.Right));
                    tr.Cells.Add(TC(p.QuantitySold.ToString($"N{dp}"), fs, TextAlignment.Center));
                    tr.Cells.Add(TC($"{p.TotalRevenue.ToString($"N{dp}")} {cur}", fs, TextAlignment.Left));
                    rg.Rows.Add(tr); rank++; alt = !alt;
                }
                t.RowGroups.Add(rg);
                doc.Blocks.Add(t);
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });
            }

            // ── مبيعات الفئات ──────────────────────────────────────
            if (secCategories && data.CategorySales?.Count > 0)
            {
                doc.Blocks.Add(SectionTitle("📂 مبيعات الفئات", isA5));
                var t = BuildFlowTable(new[] { "الفئة", "الإيرادات", "النسبة" }, fs);
                var rg = new TableRowGroup();
                bool alt = false;
                foreach (var c in data.CategorySales.OrderByDescending(x => x.TotalRevenue))
                {
                    var tr = new TableRow { Background = alt ? ColorLightBg : Brushes.White };
                    tr.Cells.Add(TC(c.CategoryName, fs, TextAlignment.Right));
                    tr.Cells.Add(TC($"{c.TotalRevenue.ToString($"N{dp}")} {cur}", fs, TextAlignment.Center));
                    tr.Cells.Add(TC($"{c.Percentage:N1}%", fs, TextAlignment.Left));
                    rg.Rows.Add(tr); alt = !alt;
                }
                t.RowGroups.Add(rg);
                doc.Blocks.Add(t);
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 8, 0, 0) });
            }

            // ─ـ المبيعات اليومية ───────────────────────────────────
            if (secDailySales && data.DailySales?.Count > 0)
            {
                doc.Blocks.Add(SectionTitle("📅 المبيعات اليومية", isA5));
                var t = BuildFlowTable(new[] { "التاريخ", "الفواتير", "الإجمالي" }, fs);
                var rg = new TableRowGroup();
                bool alt = false;
                foreach (var d in data.DailySales.OrderByDescending(x => x.Date))
                {
                    var tr = new TableRow { Background = alt ? ColorLightBg : Brushes.White };
                    tr.Cells.Add(TC(d.Date.ToString("dd/MM/yyyy"), fs, TextAlignment.Right));
                    tr.Cells.Add(TC(d.InvoicesCount.ToString(), fs, TextAlignment.Center));
                    tr.Cells.Add(TC($"{d.TotalSales.ToString($"N{dp}")} {cur}", fs, TextAlignment.Left));
                    rg.Rows.Add(tr); alt = !alt;
                }
                t.RowGroups.Add(rg);
                doc.Blocks.Add(t);
            }

            // ─ـ تذييل ──────────────────────────────────────────────
            doc.Blocks.Add(new BlockUIContainer(new Border { Height = 1, Background = ColorBorder, Margin = new Thickness(0, 20, 0, 8) }));
            doc.Blocks.Add(new Paragraph(new Run($"طُبع بتاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}"))
            {
                FontSize = fs - 2, TextAlignment = TextAlignment.Center, Foreground = ColorSecondary
            });

            return doc;
        }

        // ── دوال مساعدة للتقرير ────────────────────────────────────
        private static BlockUIContainer SectionTitle(string text, bool isA5) =>
            new BlockUIContainer(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                Padding    = new Thickness(10, 6, 10, 6),
                Margin     = new Thickness(0, 8, 0, 4),
                Child      = new TextBlock
                {
                    Text       = text,
                    FontSize   = isA5 ? 12 : 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                }
            });

        private static Table BuildFlowTable(string[] headers, int fs)
        {
            var t = new Table
            {
                CellSpacing      = 0,
                BorderBrush      = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness  = new Thickness(1)
            };
            foreach (var _ in headers)
                t.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var hg  = new TableRowGroup();
            var htr = new TableRow { Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)) };
            foreach (var h in headers)
                htr.Cells.Add(new TableCell(new Paragraph(new Run(h)) { Margin = new Thickness(0) })
                {
                    FontWeight      = FontWeights.Bold,
                    Foreground      = Brushes.White,
                    FontSize        = fs,
                    Padding         = new Thickness(6, 4, 6, 4),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(0, 0, 1, 0)
                });
            hg.Rows.Add(htr);
            t.RowGroups.Add(hg);
            return t;
        }

        private static TableCell TC(string text, int fs, TextAlignment align = TextAlignment.Center) =>
            new TableCell(new Paragraph(new Run(text ?? "")) { Margin = new Thickness(0), TextAlignment = align })
            {
                FontSize        = fs,
                Padding         = new Thickness(6, 4, 6, 4),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
    }
}
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using WpfColor = System.Windows.Media.Color;
using PdfColor = QuestPDF.Infrastructure.Color;
using PdfColors = QuestPDF.Helpers.Colors;
using System.Windows;
using System.Windows.Media;
using POS_BIGA.Infrastructure.Printing.Settings;

namespace POS_BIGA.Infrastructure.Printing.Pdf
{
    public static class FlowDocumentToPdfExporter
    {
        static FlowDocumentToPdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ─── Public contract ──────────────────────────────────────────────────────

        /// <summary>
        /// يحتوي على بيانات المستند المستخرجة من FlowDocument.
        /// آمن للتمرير إلى background threads.
        /// </summary>
        public sealed class ExtractedPdfContent
        {
            internal Action<string, PrintSettings> GenerateAction { get; set; }
        }

        /// <summary>
        /// استخراج بيانات FlowDocument — يجب استدعاؤه من UI Thread.
        /// </summary>
        public static ExtractedPdfContent ExtractContent(FlowDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var content = ExtractStructuredContent(document);  // يجب أن يكون على UI Thread
            return new ExtractedPdfContent
            {
                GenerateAction = (filePath, settings) => BuildAndSavePdf(content, filePath, settings)
            };
        }

        /// <summary>
        /// توليد PDF من بيانات مستخرجة — آمن من أي thread.
        /// </summary>
        public static void GeneratePdf(ExtractedPdfContent extracted, string filePath, PrintSettings settings = null)
        {
            if (extracted == null) throw new ArgumentNullException(nameof(extracted));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required", nameof(filePath));
            extracted.GenerateAction(filePath, settings);
        }

        /// <summary>
        /// تصدير مباشر — يجب استدعاؤه من UI Thread.
        /// </summary>
        public static void ExportToPdf(FlowDocument document, string filePath, PrintSettings settings = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required", nameof(filePath));
            BuildAndSavePdf(ExtractStructuredContent(document), filePath, settings);
        }

        public static void ExportToPdf(FlowDocument document, string filePath)
            => ExportToPdf(document, filePath, null);

        // ─── PDF construction ─────────────────────────────────────────────────────

        private static void BuildAndSavePdf(DocumentContent content, string filePath, PrintSettings settings)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(11).DirectionFromRightToLeft());

                    if (!string.IsNullOrEmpty(settings?.ShopName))
                    {
                        page.Header()
                            .BorderBottom(2).BorderColor(PdfColors.Red.Darken2)
                            .Padding(10)
                            .Column(col =>
                            {
                                col.Item().AlignCenter().Text(settings.ShopName)
                                    .FontSize(20).Bold().FontColor(PdfColors.Red.Darken2);

                                if (!string.IsNullOrEmpty(settings.ShopAddress))
                                    col.Item().AlignCenter().Text(settings.ShopAddress)
                                        .FontSize(10).FontColor(PdfColors.Grey.Darken1);

                                if (!string.IsNullOrEmpty(settings.ShopPhone))
                                    col.Item().AlignCenter().Text("هاتف: " + settings.ShopPhone)
                                        .FontSize(10).FontColor(PdfColors.Grey.Darken1);
                            });
                    }

                    page.Content().Column(col => BuildContent(col, content));

                    page.Footer()
                        .BorderTop(1).BorderColor(PdfColors.Grey.Lighten2)
                        .Padding(10).AlignCenter()
                        .Text("تم إنشاء التقرير: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
                        .FontSize(9).FontColor(PdfColors.Grey.Medium);
                });
            });

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));
            doc.GeneratePdf(filePath);
        }

        // ─── Content extraction (UI thread) ──────────────────────────────────────

        private static DocumentContent ExtractStructuredContent(FlowDocument document)
        {
            var content = new DocumentContent();
            foreach (var block in document.Blocks)
            {
                if (block is Paragraph para)        ExtractParagraph(para, content);
                else if (block is Section section)  ExtractSection(section, content);
                else if (block is Table table)      ExtractTable(table, content);
            }
            return content;
        }

        private static void ExtractParagraph(Paragraph para, DocumentContent content)
        {
            var text = new TextRange(para.ContentStart, para.ContentEnd).Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            content.Items.Add(new ContentItem
            {
                Type = ContentType.Paragraph,
                Text = text,
                FontSize = para.FontSize,
                IsBold = para.FontWeight == FontWeights.Bold || para.FontWeight == FontWeights.SemiBold,
                Alignment = para.TextAlignment,
                Foreground = (para.Foreground as SolidColorBrush)?.Color
            });
        }

        private static void ExtractSection(Section section, DocumentContent content)
        {
            foreach (var block in section.Blocks)
            {
                if (block is Paragraph para)    ExtractParagraph(para, content);
                else if (block is Table table)  ExtractTable(table, content);
            }
        }

        private static void ExtractTable(Table table, DocumentContent content)
        {
            var item = new ContentItem { Type = ContentType.Table, TableData = new TableData() };

            foreach (var rg in table.RowGroups)
            {
                foreach (TableRow row in rg.Rows)
                {
                    var cells = new List<string>();
                    foreach (TableCell cell in row.Cells)
                        cells.Add(new TextRange(cell.ContentStart, cell.ContentEnd).Text?.Trim() ?? string.Empty);

                    item.TableData.Rows.Add(cells);

                    if (rg == table.RowGroups.FirstOrDefault() && item.TableData.Rows.Count == 1)
                        item.TableData.HasHeader = true;
                }
            }

            if (item.TableData.Rows.Any()) content.Items.Add(item);
        }

        // ─── PDF rendering (any thread) ───────────────────────────────────────────

        private static void BuildContent(ColumnDescriptor col, DocumentContent content)
        {
            foreach (var item in content.Items)
            {
                if (item.Type == ContentType.Paragraph) BuildParagraph(col, item);
                else if (item.Type == ContentType.Table) BuildTable(col, item.TableData);

                col.Item().PaddingBottom(5);
            }
        }

        private static void BuildParagraph(ColumnDescriptor col, ContentItem item)
        {
            var container = col.Item();

            var aligned = item.Alignment switch
            {
                System.Windows.TextAlignment.Center => container.AlignCenter(),
                System.Windows.TextAlignment.Left => container.AlignLeft(),
                _ => container.AlignRight()
            };

            var txt = aligned.Text(item.Text);

            if (item.FontSize >= 8)
                txt = txt.FontSize((int)Math.Min(item.FontSize, 20));

            if (item.IsBold) txt = txt.Bold();

            if (item.Foreground.HasValue)
            {
                var c = item.Foreground.Value;
                txt = txt.FontColor(PdfColor.FromRGB(c.R, c.G, c.B));
            }
        }

        private static void BuildTable(ColumnDescriptor col, TableData tableData)
        {
            if (!tableData.Rows.Any()) return;
            var colCount = tableData.Rows.First().Count;

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cols => { for (int i = 0; i < colCount; i++) cols.RelativeColumn(); });

                if (tableData.HasHeader)
                {
                    var headerRow = tableData.Rows.First();
                    t.Header(h =>
                    {
                        foreach (var cell in headerRow)
                            h.Cell().Background(PdfColors.Blue.Darken2).Padding(8)
                             .AlignCenter().AlignMiddle()
                             .Text(cell).Bold().FontColor(PdfColors.White).FontSize(11);
                    });

                    foreach (var row in tableData.Rows.Skip(1))
                        foreach (var cell in row)
                            t.Cell().Border(1).BorderColor(PdfColors.Grey.Lighten2)
                             .Padding(6).AlignCenter().AlignMiddle().Text(cell).FontSize(10);
                }
                else
                {
                    foreach (var row in tableData.Rows)
                        foreach (var cell in row)
                            t.Cell().Border(1).BorderColor(PdfColors.Grey.Lighten2)
                             .Padding(6).AlignCenter().AlignMiddle().Text(cell).FontSize(10);
                }
            });
        }

        // ─── Internal data models ─────────────────────────────────────────────────

        private sealed class DocumentContent
        {
            public List<ContentItem> Items { get; } = new();
        }

        private sealed class ContentItem
        {
            public ContentType Type { get; set; }
            public string Text { get; set; }
            public double FontSize { get; set; }
            public bool IsBold { get; set; }
            public System.Windows.TextAlignment Alignment { get; set; }
            public WpfColor? Foreground { get; set; }
            public TableData TableData { get; set; }
        }

        private enum ContentType { Paragraph, Table }

        private sealed class TableData
        {
            public List<List<string>> Rows { get; } = new();
            public bool HasHeader { get; set; }
        }
    }
}

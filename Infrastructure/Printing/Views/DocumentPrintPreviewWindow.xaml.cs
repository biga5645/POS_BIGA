using POS_BIGA.Infrastructure.Printing.Services;
using System;
using System.Windows;
using System.Windows.Documents;

namespace POS_BIGA.Infrastructure.Printing.Views
{
    public partial class DocumentPrintPreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly string _documentTitle;
        private readonly string? _targetPrinter;

        public DocumentPrintPreviewWindow(FlowDocument document, string documentTitle, string? targetPrinter = null)
        {
            InitializeComponent();
            _document      = document ?? throw new ArgumentNullException(nameof(document));
            _documentTitle = string.IsNullOrWhiteSpace(documentTitle) ? "مستند" : documentTitle;
            _targetPrinter = targetPrinter;
            TitleText.Text = $"معاينة: {_documentTitle}";
            PreviewViewer.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrinterService.Instance.PrintDocument(_document, _targetPrinter);
                MessageBox.Show(" ✅ تم تأكيد الطباعة بنجاح", "تأكيد الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ فشلت الطباعة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

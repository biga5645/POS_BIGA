using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Models;
using System;
using System.Windows;
using System.Windows.Documents;

namespace POS_BIGA.Infrastructure.Printing.Views
{
    public partial class SimpleTemplatePreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly PrinterService _printerService;

        public SimpleTemplatePreviewWindow(Sale sale, Settings.PrintSettings settings)
        {
            InitializeComponent();
            _printerService = PrinterService.Instance;
            var template = _printerService.GetTemplateByName("Simple");
            _document = template?.BuildPreview(sale, settings)
                       ?? throw new InvalidOperationException("قالب Simple غير موجود ");
            PreviewViewer.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = _printerService.LoadSettings();
                var targetPrinter = string.IsNullOrWhiteSpace(settings.ReceiptPrinter) ? null : settings.ReceiptPrinter;
                _printerService.PrintDocument(_document, targetPrinter);
                MessageBox.Show("✅ تم تأكيد الطباعة بنجاح", " تأكيد الطباعة",
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

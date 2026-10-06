using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Models;
using System;
using System.Windows;
using System.Windows.Documents;

namespace POS_BIGA.Infrastructure.Printing.Views
{
    public partial class ProfessionalTemplatePreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly PrinterService _printerService;

        public ProfessionalTemplatePreviewWindow(Sale sale, Settings.PrintSettings settings)
        {
            InitializeComponent();
            _printerService = PrinterService.Instance;
            var template = _printerService.GetTemplateByName("Professional A4/A5");
            _document = template?.BuildPreview(sale, settings)
                       ?? throw new InvalidOperationException("???? Professional A4/A5 ??? ?????");
            PreviewViewer.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = _printerService.LoadSettings();
                var targetPrinter = string.IsNullOrWhiteSpace(settings.ReceiptPrinter) ? null : settings.ReceiptPrinter;
                _printerService.PrintDocument(_document, targetPrinter);
                MessageBox.Show("? ?? ????? ??????? ?????", "????? ???????",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"? ???? ???????:\n{ex.Message}", "???",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

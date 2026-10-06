using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Models;

namespace POS_BIGA.Infrastructure.Printing.Abstractions
{
    public interface IPrinterService
    {
        IEnumerable<string> GetInstalledPrinters();
        string GetDefaultPrinter();

        FlowDocument BuildReceiptDocument(Sale sale, PrintSettings settings = null);

        void PrintDocument(FlowDocument document, string printerName = null);
        Task PrintDocumentAsync(FlowDocument document, string printerName = null);

        void PrintReceipt(Sale sale, string printerName = null, decimal taxRatePercent = 0m);
        Task PrintReceiptAsync(Sale sale, string printerName = null, decimal taxRatePercent = 0m);

        void PrintReceipt(Sale sale, PrintSettings settings, string printerName = null, decimal taxRatePercent = 0m);
        Task PrintReceiptAsync(Sale sale, PrintSettings settings, string printerName = null, decimal taxRatePercent = 0m);

        void PrintText(string text, string printerName = null);
        Task PrintTextAsync(string text, string printerName = null);

        // Load settings from storage (sync/async)
        PrintSettings LoadSettings();
        Task<PrintSettings> LoadSettingsAsync();

        // list available templates
        IEnumerable<string> GetAvailableTemplates();
    }
}

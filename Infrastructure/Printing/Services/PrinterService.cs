using POS_BIGA.Infrastructure.Printing.Abstractions;
using POS_BIGA.Infrastructure.Printing.Engines;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Infrastructure.Printing.Templates;
using POS_BIGA.Models;
using POS_BIGA.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using POS_BIGA.Services;

namespace POS_BIGA.Infrastructure.Printing.Services
{
    /// <summary>
    /// Singleton PrinterService with cached settings
    /// </summary>
    public sealed class PrinterService : IPrinterService
    {
        // ===========================
        // SINGLETON PATTERN
        // ===========================
        private static readonly Lazy<PrinterService> _instance = new Lazy<PrinterService>(() => new PrinterService());
        public static PrinterService Instance => _instance.Value;

        // ===========================
        // CACHED SETTINGS
        // ===========================
        private PrintSettings _cachedSettings;
        private readonly object _settingsLock = new object();

        private readonly Dictionary<string, IPrintTemplate> _templates = new();

        private PrinterService()
        {
            RegisterTemplate(new SimpleTemplate());
            RegisterTemplate(new CompactTemplate());

            // Load settings on initialization
            _ = LoadSettingsAsync();
        }

        public void RegisterTemplate(IPrintTemplate template)
        {
            if (template == null) return;
            _templates[template.Name] = template;
        }

        /// <summary>
        /// Clear cached settings - force reload from DB
        /// </summary>
        public void InvalidateCache()
        {
            lock (_settingsLock)
            {
                _cachedSettings = null;
                Debug.WriteLine("🔄 PrinterService: Cache invalidated");
            }
        }

        /// <summary>
        /// Refresh settings from database and update cache
        /// </summary>
        public async Task RefreshSettingsAsync()
        {
            try
            {
                Debug.WriteLine("🔄 PrinterService: Refreshing settings from DB...");

                using var ctx = new AppDbContext();
                var s = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(ctx.AppSettings);
                var settings = new PrintSettings();

                if (s != null)
                {
                    settings.ShopName = s.ShopName ?? string.Empty;
                    settings.ShopAddress = s.ShopAddress ?? string.Empty;
                    settings.ShopPhone = s.ShopPhone ?? string.Empty;
                    settings.Currency = s.Currency ?? "MAD";
                    settings.TaxRate = s.TaxRate;
                    settings.InvoiceMessage = s.InvoiceMessage ?? string.Empty;
                }
                else
                {
                    Debug.WriteLine("⚠️ PrinterService: No settings found in DB - using defaults");
                }

                var tpl = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.AppSettings.Where(a => a.SettingKey == "PrintTemplate"));
                if (tpl != null) settings.TemplateName = tpl.SettingValue ?? settings.TemplateName;

                var rp = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.AppSettings.Where(a => a.SettingKey == "ReceiptPrinter"));
                if (rp != null) settings.ReceiptPrinter = rp.SettingValue ?? settings.ReceiptPrinter;

                var rw = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.AppSettings.Where(a => a.SettingKey == "ReceiptWidthMm"));
                if (rw != null && int.TryParse(rw.SettingValue, out var mm)) settings.ReceiptWidthMm = mm;

                var ap = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.AppSettings.Where(a => a.SettingKey == "AutoPrint"));
                if (ap != null) settings.AutoPrint = (ap.SettingValue == "1" || ap.SettingValue?.ToLower() == "true");

                lock (_settingsLock)
                {
                    _cachedSettings = settings;
                }

                Debug.WriteLine($"✅ PrinterService: Settings cached");
                Debug.WriteLine($"   ShopName: '{settings.ShopName}'");
                Debug.WriteLine($"   ReceiptPrinter: '{settings.ReceiptPrinter}'");
                Debug.WriteLine($"   TemplateName: '{settings.TemplateName}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ PrinterService: Failed to refresh settings: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");

                // Create default settings on error
                lock (_settingsLock)
                {
                    _cachedSettings = new PrintSettings
                    {
                        ShopName = "متجري",
                        TemplateName = "Simple",
                        ReceiptWidthMm = 80
                    };
                }

                throw; // Re-throw to notify caller
            }
        }

        public IEnumerable<string> GetInstalledPrinters()
        {
            try
            {
                var server = new LocalPrintServer();
                return server.GetPrintQueues().Select(q => q.FullName).OrderBy(n => n);
            }
            catch { return Enumerable.Empty<string>(); }
        }

        public string GetDefaultPrinter()
        {
            try
            {
                var server = new LocalPrintServer();
                return server.DefaultPrintQueue?.FullName;
            }
            catch { return null; }
        }

        public FlowDocument BuildReceiptDocument(Sale sale, PrintSettings settings = null)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            settings ??= new PrintSettings();
            var tpl = GetTemplateByName(settings.TemplateName ?? string.Empty);
            if (tpl == null) throw new InvalidOperationException("Print template not found");
            return tpl.BuildPreview(sale, settings);
        }

        private void NotifyPrintingStatus(string message)
        {
            try
            {
                Debug.WriteLine("PrinterService: " + message);
                var appState = Application.Current?.Resources["AppStateService"] as IAppStateService;
                appState?.NotifyPrintingStatus(message);
            }
            catch { }
        }

        public void PrintDocument(FlowDocument document, string printerName = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            Debug.WriteLine("═══════════════════════════════════════");
            Debug.WriteLine($"🖨️ PrintDocument called");
            Debug.WriteLine($"   printerName parameter: '{printerName}'");

            var target = printerName;

            // If no printer specified, use default
            if (string.IsNullOrEmpty(target))
            {
                target = GetDefaultPrinter();
                Debug.WriteLine($"⚠️ No printer specified, falling back to default: '{target}'");
            }
            else
            {
                Debug.WriteLine($"✅ Using specified printer: '{target}'");
            }

            // Check if target printer exists in installed printers
            var installedPrinters = GetInstalledPrinters().ToList();
            Debug.WriteLine($"📋 Installed printers: {string.Join(", ", installedPrinters.Select(p => $"'{p}'"))}");


            if (!string.IsNullOrEmpty(target) && !installedPrinters.Any(p => string.Equals(p, target, StringComparison.OrdinalIgnoreCase)))
            {
                var errorMsg = $"⚠️ Printer '{target}' not found in installed printers. Available: {string.Join(", ", installedPrinters)}";
                Debug.WriteLine(errorMsg);
                NotifyPrintingStatus(errorMsg);

                // Try to use default printer as fallback
                var defaultPrinter = GetDefaultPrinter();
                if (!string.IsNullOrEmpty(defaultPrinter))
                {
                    Debug.WriteLine($"🔄 Falling back to default printer: '{defaultPrinter}'");
                    Debug.WriteLine($"Printer '{target}' not available. Using default: '{defaultPrinter}'");
                    target = defaultPrinter;
                }
            }

            if (string.IsNullOrEmpty(target))
            {
                var errorMsg = "❌ No printer available (no default printer and specified printer not found)";
                Debug.WriteLine(errorMsg);
                NotifyPrintingStatus(errorMsg);

                // Show user-friendly message
                var installedList = string.Join("\n• ", installedPrinters);
                MessageBox.Show(
                    $"لا توجد طابعة متاحة!\n\n" +
                    $"الطابعة المحددة: '{printerName}'\n" +
                    $"الطابعة الافتراضية: '{GetDefaultPrinter()}'\n\n" +
                    $"الطوابع المثبتة:\n• {installedList}\n\n" +
                    $"الرجاء:\n" +
                    $"1. تأكد من اتصال الطابعة\n" +
                    $"2. افتح الإعدادات واختر طابعة من القائمة\n" +
                    $"3. تأكد من أن الاسم مطابق تماماً",
                    "خطأ في الطباعة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                throw new InvalidOperationException("No printer available");
            }

            Debug.WriteLine($"✅ Using printer: '{target}'");
            Debug.WriteLine($"📄 PrintDocument: printerName='{printerName}', target='{target}', default='{GetDefaultPrinter()}'");

            Debug.WriteLine($"Printing document via XPS to '{target}'...");

            // try to determine printable width and apply to document before pagination
            double printableWidth = double.NaN;
            try
            {
                var serverProbe = new LocalPrintServer();
                PrintQueue probeQueue = null;
                try { probeQueue = serverProbe.GetPrintQueue(target); } catch { }
                if (probeQueue != null)
                {
                    try
                    {
                        var caps = probeQueue.GetPrintCapabilities();
                        var area = caps?.PageImageableArea;
                        if (area != null)
                        {
                            printableWidth = area.ExtentWidth;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // fallback to document width or reasonable default
            if (double.IsNaN(printableWidth) || printableWidth <= 0)
            {
                printableWidth = double.IsNaN(document.PageWidth) || document.PageWidth <= 0 ? 300.0 : document.PageWidth;
            }

            try
            {
                document.PageWidth = printableWidth;
                try { document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right; } catch { }
            }
            catch { }

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;

            try
            {
                var server = new LocalPrintServer();
                PrintQueue queue = null;
                try { queue = server.GetPrintQueue(target); } catch { }
                if (queue != null)
                {
                    try
                    {
                        // Ensure paginator page size matches printer printable area to avoid clipping / weird layout
                        try
                        {
                            var capabilities = queue.GetPrintCapabilities();
                            var area = capabilities.PageImageableArea;
                            if (area != null)
                            {
                                // use printable width and a large height so receipt can be long
                                var printableHeight = Math.Max(area.ExtentHeight, 2000.0);
                                paginator.PageSize = new System.Windows.Size(area.ExtentWidth, printableHeight);
                            }
                            else
                            {
                                // fallback to document width and a large height
                                var pw = double.IsNaN(document.PageWidth) || document.PageWidth <= 0 ? 300.0 : document.PageWidth;
                                paginator.PageSize = new System.Windows.Size(pw, 2000.0);
                            }
                        }
                        catch { /* ignore capability probing failures */ }

                        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
                        writer.Write(paginator);
                        Debug.WriteLine($"Printed via XPS to '{queue.FullName}'");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Print via XPS writer failed for '{queue?.FullName}': {ex.Message}");
                        Debug.WriteLine(ex);
                        // try PrintDialog on STA thread
                        try
                        {
                            var pd = new System.Windows.Controls.PrintDialog();
                            pd.PrintQueue = queue;
                            pd.PrintDocument(paginator, "Receipt");
                            Debug.WriteLine($"Printed via PrintDialog to '{queue.FullName}'");
                            return;
                        }
                        catch (Exception ex2)
                        {
                            Debug.WriteLine($"Fallback PrintDialog failed for '{queue?.FullName}': {ex2.Message}");
                            Debug.WriteLine(ex2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            // fallback to default printer
            try
            {
                var defServer = new LocalPrintServer();
                var defQueue = defServer.DefaultPrintQueue;
                if (defQueue != null)
                {
                    try
                    {
                        // try to set page size for default printer as well
                        try
                        {
                            var caps = defQueue.GetPrintCapabilities();
                            var area = caps.PageImageableArea;
                            if (area != null)
                            {
                                paginator.PageSize = new System.Windows.Size(area.ExtentWidth, Math.Max(area.ExtentHeight, 2000.0));
                            }
                            else
                            {
                                var pw = double.IsNaN(document.PageWidth) || document.PageWidth <= 0 ? 300.0 : document.PageWidth;
                                paginator.PageSize = new System.Windows.Size(pw, 2000.0);
                            }
                        }
                        catch { }

                        var writer2 = PrintQueue.CreateXpsDocumentWriter(defQueue);
                        writer2.Write(paginator);
                        Debug.WriteLine($"Printed via XPS to default printer '{defQueue.FullName}'");
                        return;
                    }
                    catch (Exception ex)
                    {
                        NotifyPrintingStatus($"⚠️ فشلت الطباعة: {ex.Message}");
                        Debug.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            NotifyPrintingStatus("⚠️ فشلت الطباعة على جميع الطابعات.");
            throw new InvalidOperationException("Failed to print document to any printer.");
        }

        public void PrintReceipt(Sale sale, PrintSettings settings, string printerName = null, decimal taxRatePercent = 0m)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            settings ??= new PrintSettings();
            settings.TaxRate = taxRatePercent;
            var tpl = GetTemplateByName(settings.TemplateName ?? string.Empty);
            if (tpl == null) throw new InvalidOperationException("Print template not found");

            Debug.WriteLine($"Starting print for sale {sale.Id} using template '{tpl.Name}'");
            Debug.WriteLine($"🖨️ Print settings: ReceiptPrinter='{settings.ReceiptPrinter}', printerName parameter='{printerName}', AutoPrint={settings.AutoPrint}");

            try
            {
                Debug.WriteLine("Attempting RAW output from template...");
                var raw = tpl.BuildRaw(sale, settings);
                if (raw != null && raw.Length > 0)
                {
                    // Ensure ReceiptPrinter is used if specified
                    string targetPrinter = null;

                    if (!string.IsNullOrEmpty(settings.ReceiptPrinter))
                    {
                        Debug.WriteLine($"✅ Using specified ReceiptPrinter: '{settings.ReceiptPrinter}'");
                        targetPrinter = settings.ReceiptPrinter;
                    }
                    else if (!string.IsNullOrEmpty(printerName))
                    {
                        Debug.WriteLine($"✅ Using printerName parameter: '{printerName}'");
                        targetPrinter = printerName;
                    }
                    else
                    {
                        Debug.WriteLine($"⚠️ No printer specified, falling back to default");
                        targetPrinter = GetDefaultPrinter();
                    }

                    Debug.WriteLine($"🎯 Final target printer: '{targetPrinter}'");

                    if (!string.IsNullOrEmpty(targetPrinter))
                    {
                        Debug.WriteLine($"Sending RAW bytes to printer '{targetPrinter}'...");
                        var ok = RawPrinterHelper.SendBytesToPrinter(targetPrinter, raw);
                        if (ok)
                        {
                            Debug.WriteLine($"RAW send succeeded to '{targetPrinter}'. Printing complete.");
                            return;
                        }
                        else
                        {
                            Debug.WriteLine($"RAW send failed to '{targetPrinter}'. Falling back to XPS.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No target printer specified for RAW output. Falling back to XPS.");
                    }
                }
                else
                {
                    Debug.WriteLine("Template produced no RAW data. Falling back to XPS.");
                }
            }
            catch (NotSupportedException)
            {
                Debug.WriteLine("Template does not support RAW output. Using XPS for printing.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while attempting RAW print: {ex.Message}. Falling back to XPS.");
                Debug.WriteLine(ex);
            }

            Debug.WriteLine("Building FlowDocument via template for XPS printing...");
            FlowDocument doc = null;
            try
            {
                var app = Application.Current;
                if (app != null && app.Dispatcher != null)
                {
                    app.Dispatcher.Invoke(() => { doc = tpl.BuildPreview(sale, settings); });
                }
                else
                {
                    // fallback if no application dispatcher: build on current thread
                    doc = tpl.BuildPreview(sale, settings);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to build preview on UI thread: {ex.Message}");
                Debug.WriteLine(ex);
                // as a last resort try building on current thread
                doc = tpl.BuildPreview(sale, settings);
            }
            try
            {
                // Ensure printing that accesses the FlowDocument runs on the same thread that owns it
                var app = Application.Current;
                if (app != null && app.Dispatcher != null)
                {
                    app.Dispatcher.Invoke(() => {
                        try
                        {
                            PrintDocument(doc, settings.ReceiptPrinter ?? printerName);
                        }
                        catch (Exception pexInner)
                        {
                            NotifyPrintingStatus($"⚠️ فشلت الطباعة: {pexInner.Message}");
                            Debug.WriteLine(pexInner);
                        }
                    });
                }
                else
                {
                    PrintDocument(doc, settings.ReceiptPrinter ?? printerName);
                }
            }
            catch (Exception pex)
            {
                NotifyPrintingStatus($"⚠️ فشلت الطباعة: {pex.Message}");
                Debug.WriteLine(pex);
            }
        }

        public void PrintReceipt(Sale sale, string printerName = null, decimal taxRatePercent = 0m)
        {
            Debug.WriteLine("═══════════════════════════════════════");
            Debug.WriteLine("🖨️ PrintReceipt called (loading settings from DB)");
            Debug.WriteLine($"   printerName parameter: '{printerName}'");

            var settings = LoadSettings();

            Debug.WriteLine($"   Loaded ReceiptPrinter from DB: '{settings.ReceiptPrinter}'");
            Debug.WriteLine($"   Will use printer: '{settings.ReceiptPrinter ?? printerName ?? "DEFAULT"}'");
            Debug.WriteLine("═══════════════════════════════════════");

            PrintReceipt(sale, settings, printerName, taxRatePercent);
        }

        public async Task PrintReceiptAsync(Sale sale, string printerName = null, decimal taxRatePercent = 0m)
        {
            await Task.Run(() => PrintReceipt(sale, printerName, taxRatePercent));
        }

        public async Task PrintReceiptAsync(Sale sale, PrintSettings settings, string printerName = null, decimal taxRatePercent = 0m)
        {
            await Task.Run(() => PrintReceipt(sale, settings, printerName, taxRatePercent));
        }

        public void PrintText(string text, string printerName = null)
        {
            var doc = new FlowDocument(new Paragraph(new Run(text ?? string.Empty))) { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"), FontSize = 12 };
            PrintDocument(doc, printerName);
        }

        public Task PrintDocumentAsync(FlowDocument document, string printerName = null)
        {
            return Task.Run(() => PrintDocument(document, printerName));
        }

        public Task PrintTextAsync(string text, string printerName = null)
        {
            return Task.Run(() => PrintText(text, printerName));
        }

        public PrintSettings LoadSettings()
        {
            lock (_settingsLock)
            {
                if (_cachedSettings != null)
                {
                    Debug.WriteLine("✅ PrinterService: Using cached settings");
                    return _cachedSettings;
                }
            }

            Debug.WriteLine("⚠️ PrinterService: Cache miss - loading from DB synchronously");

            // This should rarely happen - only on first call before async load completes
            try
            {
                using var ctx = new AppDbContext();
                var s = ctx.AppSettings.FirstOrDefault();
                var settings = new PrintSettings();

                if (s != null)
                {
                    settings.ShopName = s.ShopName ?? string.Empty;
                    settings.ShopAddress = s.ShopAddress ?? string.Empty;
                    settings.ShopPhone = s.ShopPhone ?? string.Empty;
                    settings.Currency = s.Currency ?? "MAD";
                    settings.TaxRate = s.TaxRate;
                    settings.InvoiceMessage = s.InvoiceMessage ?? string.Empty;
                }

                var tpl = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "PrintTemplate");
                if (tpl != null) settings.TemplateName = tpl.SettingValue ?? settings.TemplateName;

                var rp = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ReceiptPrinter");
                if (rp != null) settings.ReceiptPrinter = rp.SettingValue ?? settings.ReceiptPrinter;

                Debug.WriteLine($"🔧 LoadSettings: ReceiptPrinter from DB: Key='{rp?.SettingKey}', Value='{rp?.SettingValue}', Result='{settings.ReceiptPrinter}'");

                var rw = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ReceiptWidthMm");
                if (rw != null && int.TryParse(rw.SettingValue, out var mm)) settings.ReceiptWidthMm = mm;

                var ap = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "AutoPrint");
                if (ap != null) settings.AutoPrint = (ap.SettingValue == "1" || ap.SettingValue?.ToLower() == "true");

                // تحميل إعدادات الطابعة الحرارية
                var tfs = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ThermalFontSize");
                if (tfs != null && int.TryParse(tfs.SettingValue, out var fontSize)) settings.ThermalFontSize = fontSize;

                var td = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ThermalDensity");
                if (td != null && int.TryParse(td.SettingValue, out var density)) settings.ThermalDensity = density;

                var sth = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ShowThermalHeader");
                if (sth != null) settings.ShowThermalHeader = (sth.SettingValue == "True" || sth.SettingValue == "true");

                var stl = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ShowThermalLines");
                if (stl != null) settings.ShowThermalLines = (stl.SettingValue == "True" || stl.SettingValue == "true");

                var stt = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "ShowThermalTotals");
                if (stt != null) settings.ShowThermalTotals = (stt.SettingValue == "True" || stt.SettingValue == "true");

                var cap = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "CutAfterPrint");
                if (cap != null) settings.CutAfterPrint = (cap.SettingValue == "True" || cap.SettingValue == "true");

                var ps = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "PaperSize");
                if (ps != null && Enum.TryParse<PaperSize>(ps.SettingValue, out var paperSize)) settings.PaperSize = paperSize;

                var dps = ctx.AppSettings.FirstOrDefault(a => a.SettingKey == "DecimalPlaces");
                if (dps != null && int.TryParse(dps.SettingValue, out var decimalPlaces)) settings.DecimalPlaces = decimalPlaces;

                lock (_settingsLock)
                {
                    _cachedSettings = settings;
                }

                return settings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ PrinterService: LoadSettings failed: {ex.Message}");
                Debug.WriteLine($"   Stack trace: {ex.StackTrace}");

                var defaultSettings = new PrintSettings
                {
                    ShopName = "متجري",
                    TemplateName = "Simple",
                    ReceiptWidthMm = 80
                };

                lock (_settingsLock)
                {
                    _cachedSettings = defaultSettings;
                }

                return defaultSettings;
            }
        }

        public async Task<PrintSettings> LoadSettingsAsync()
        {
            lock (_settingsLock)
            {
                if (_cachedSettings != null)
                {
                    Debug.WriteLine("✅ PrinterService: Using cached settings (async)");
                    return Task.FromResult(_cachedSettings).Result;
                }
            }

            await RefreshSettingsAsync();

            lock (_settingsLock)
            {
                return _cachedSettings ?? new PrintSettings();
            }
        }

        public IEnumerable<string> GetAvailableTemplates() => _templates.Keys.OrderBy(k => k);

        public IPrintTemplate GetTemplateByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_templates.TryGetValue(name, out var tpl)) return tpl;
            return null;
        }

        /// <summary>
        /// بناء معاينة FlowDocument للفاتورة
        /// </summary>
        public FlowDocument BuildPreview(Sale sale, PrintSettings settings)
        {
            try
            {
                var tpl = GetTemplateByName(settings.TemplateName);
                if (tpl == null)
                {
                    tpl = _templates.Values.FirstOrDefault();
                    if (tpl == null)
                    {
                        throw new InvalidOperationException("No print templates available");
                    }
                }

                FlowDocument doc = null;
                var app = Application.Current;

                if (app != null && app.Dispatcher != null)
                {
                    app.Dispatcher.Invoke(() =>
                    {
                        doc = tpl.BuildPreview(sale, settings);
                    });
                }
                else
                {
                    doc = tpl.BuildPreview(sale, settings);
                }

                return doc;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ BuildPreview failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// حفظ إعدادات الطباعة
        /// </summary>
        public async Task SaveSettingsAsync(PrintSettings settings)
        {
            try
            {
                Debug.WriteLine("💾 SaveSettingsAsync: بدء الحفظ...");

                using var ctx = new AppDbContext();

                // حفظ Template Name
                await SaveOrUpdateSettingAsync(ctx, "PrintTemplate", settings.TemplateName);
                Debug.WriteLine($"   ✅ PrintTemplate: {settings.TemplateName}");

                // حفظ Receipt Width
                await SaveOrUpdateSettingAsync(ctx, "ReceiptWidthMm", settings.ReceiptWidthMm.ToString());
                Debug.WriteLine($"   ✅ ReceiptWidthMm: {settings.ReceiptWidthMm}");

                // حفظ Decimal Places
                await SaveOrUpdateSettingAsync(ctx, "DecimalPlaces", settings.DecimalPlaces.ToString());
                Debug.WriteLine($"   ✅ DecimalPlaces: {settings.DecimalPlaces}");

                // حفظ Thermal Settings
                await SaveOrUpdateSettingAsync(ctx, "ThermalFontSize", settings.ThermalFontSize.ToString());
                await SaveOrUpdateSettingAsync(ctx, "ThermalDensity", settings.ThermalDensity.ToString());
                await SaveOrUpdateSettingAsync(ctx, "ShowThermalHeader", settings.ShowThermalHeader.ToString());
                await SaveOrUpdateSettingAsync(ctx, "ShowThermalLines", settings.ShowThermalLines.ToString());
                await SaveOrUpdateSettingAsync(ctx, "ShowThermalTotals", settings.ShowThermalTotals.ToString());
                await SaveOrUpdateSettingAsync(ctx, "CutAfterPrint", settings.CutAfterPrint.ToString());

                // حفظ Paper Size
                await SaveOrUpdateSettingAsync(ctx, "PaperSize", settings.PaperSize.ToString());
                Debug.WriteLine($"   ✅ PaperSize: {settings.PaperSize}");

                await ctx.SaveChangesAsync();

                // تحديث Cache
                lock (_settingsLock)
                {
                    _cachedSettings = settings;
                }

                Debug.WriteLine("✅ SaveSettingsAsync: تم الحفظ بنجاح");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ SaveSettingsAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// حفظ إعدادات الطباعة (Sync)
        /// </summary>
        public void SaveSettings(PrintSettings settings)
        {
            SaveSettingsAsync(settings).Wait();
        }

        /// <summary>
        /// حفظ أو تحديث إعداد واحد
        /// </summary>
        private async Task SaveOrUpdateSettingAsync(AppDbContext ctx, string key, string value)
        {
            var setting = ctx.AppSettings.FirstOrDefault(s => s.SettingKey == key);
            if (setting != null)
            {
                setting.SettingValue = value;
                ctx.AppSettings.Update(setting);
            }
            else
            {
                ctx.AppSettings.Add(new POS_BIGA.Models.AppSetting 
                { 
                    SettingKey = key, 
                    SettingValue = value 
                });
            }
        }

        /// <summary>
        /// فتح نافذة معاينة الفاتورة
        /// </summary>
        public void ShowPreviewWindow(Sale sale, PrintSettings settings = null)
        {
            // InvoicePreviewWindow تم حذفها — تنفيذ الطباعة مباشرة
            try
            {
                var printSettings = settings ?? LoadSettings();
                PrintReceipt(sale, printSettings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowPreviewWindow fallback failed: {ex.Message}");
            }
        }
    }
}

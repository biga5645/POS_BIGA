using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Services;
using POS_BIGA.Models;
using System.Collections.ObjectModel;
using System.Linq;
using POS_BIGA.Infrastructure.Printing.Services;

namespace POS_BIGA.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IAppSettingService _settingService;

        public ObservableCollection<string> InstalledPrinters { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTemplates { get; } = new ObservableCollection<string>();

        public SettingsViewModel() : this(new AppSettingService()) { }

        public SettingsViewModel(IAppSettingService settingService)
        {
            _settingService = settingService;
            SaveCommand            = new RelayCommand(async obj => await SaveAsync(obj));
            RefreshPrintersCommand = new RelayCommand(async _ => await LoadPrintersAsync());
            PreviewTemplateCommand = new RelayCommand(_ => PreviewTemplate());
            CustomizeTemplateCommand = new RelayCommand(_ => CustomizeTemplate());
            PrintTestCommand        = new RelayCommand(_ => PrintTest());

            // ✅ تهيئة ViewModel إدارة المستخدمين
            Users = new UserManagementViewModel(
                new POS_BIGA.Services.UserService(
                    new POS_BIGA.Data.Repositories.UserRepository()));

            Permissions     = new RolePermissionsViewModel(settingService);
            Backup      = new DatabaseBackupViewModel();

            // load current settings
            _ = LoadAsync();
        }

        /// <summary>ViewModel إدارة المستخدمين - مرتبط بتبويب المستخدمين</summary>
        public UserManagementViewModel     Users       { get; }

        /// <summary>ViewModel إدارة الصلاحيات - للمدير فقط</summary>
        public RolePermissionsViewModel    Permissions { get; }

        /// <summary>ViewModel النسخ الاحتياطي واستعادة قاعدة البيانات</summary>
        public DatabaseBackupViewModel     Backup      { get; }

        /// <summary>هل المستخدم الحالي مدير؟ - لإظهار تبويب الصلاحيات</summary>
        public bool IsAdmin => SessionService.IsAdmin;

        public ICommand RefreshPrintersCommand  { get; }
        public ICommand PreviewTemplateCommand  { get; }
        public ICommand CustomizeTemplateCommand{ get; }
        public ICommand PrintTestCommand        { get; }

        private async Task LoadAsync()
        {
            try
            {
                var s = await _settingService.GetStoreSettingsAsync();
                if (s != null)
                {
                    ConnectionString = await _settingService.GetByKeyAsync("ConnectionString") is AppSetting cs ? cs.SettingValue : ConnectionString;

                    ShopName = s.ShopName;
                    ShopAddress = s.ShopAddress;
                    ShopPhone = s.ShopPhone;
                    TaxRate = s.TaxRate;
                    Currency = s.Currency;
                    CreditEnabled = s.CreditEnabled;
                    CreditLimit = s.CreditLimit;
                    InvoiceMessage = s.InvoiceMessage;
                }

                // ✅ تحميل API Base URL وتحديث الـ cache
                ApiBaseUrl = await _settingService.GetByKeyAsync("ApiBaseUrl") is AppSetting apiUrl ? apiUrl.SettingValue : string.Empty;
                POS_BIGA.Infrastructure.Helpers.ImageStorageHelper.SetBaseUrl(ApiBaseUrl);

                // load printing-related settings
                ReceiptPrinter = await _settingService.GetByKeyAsync("ReceiptPrinter") is AppSetting rp2 ? rp2.SettingValue : string.Empty;
                SelectedTemplate = await _settingService.GetByKeyAsync("PrintTemplate") is AppSetting pt ? pt.SettingValue : "Simple";
                AutoPrint = await _settingService.GetByKeyAsync("AutoPrint") is AppSetting ap ? (ap.SettingValue == "1" || ap.SettingValue?.ToLower() == "true") : false;
                ReceiptWidthMm = int.TryParse(await _settingService.GetByKeyAsync("ReceiptWidthMm") is AppSetting rw ? rw.SettingValue : "80", out var rwmm) ? rwmm : 80;
                ForceEscPos = await _settingService.GetByKeyAsync("ForceEscPos") is AppSetting fe ? (fe.SettingValue == "1" || fe.SettingValue?.ToLower() == "true") : false;

                // تحميل إعدادات أقسام التقرير الحراري
                ReportShowHeader      = await _settingService.GetByKeyAsync("ReportShowHeader")      is AppSetting rs1 ? rs1.SettingValue != "0" : true;
                ReportShowSummary     = await _settingService.GetByKeyAsync("ReportShowSummary")     is AppSetting rs2 ? rs2.SettingValue != "0" : true;
                ReportShowTopProducts = await _settingService.GetByKeyAsync("ReportShowTopProducts") is AppSetting rs3 ? rs3.SettingValue != "0" : true;
                ReportShowCategories  = await _settingService.GetByKeyAsync("ReportShowCategories")  is AppSetting rs4 ? rs4.SettingValue != "0" : true;
                ReportShowDailySales  = await _settingService.GetByKeyAsync("ReportShowDailySales")  is AppSetting rs5 ? rs5.SettingValue != "0" : true;
                ReportShowCredits     = await _settingService.GetByKeyAsync("ReportShowCredits")     is AppSetting rs6 ? rs6.SettingValue != "0" : true;
                ReportShowCreditsList = await _settingService.GetByKeyAsync("ReportShowCreditsList") is AppSetting rs7 ? rs7.SettingValue == "1" : false;

                // تحميل إعدادات أقسام تقرير A4
                A4ReportShowHeader      = await _settingService.GetByKeyAsync("A4ReportShowHeader")      is AppSetting a1 ? a1.SettingValue != "0" : true;
                A4ReportShowSummary     = await _settingService.GetByKeyAsync("A4ReportShowSummary")     is AppSetting a2 ? a2.SettingValue != "0" : true;
                A4ReportShowTopProducts = await _settingService.GetByKeyAsync("A4ReportShowTopProducts") is AppSetting a3 ? a3.SettingValue != "0" : true;
                A4ReportShowCategories  = await _settingService.GetByKeyAsync("A4ReportShowCategories")  is AppSetting a4 ? a4.SettingValue != "0" : true;
                A4ReportShowDailySales  = await _settingService.GetByKeyAsync("A4ReportShowDailySales")  is AppSetting a5 ? a5.SettingValue != "0" : true;
                A4ReportShowCredits     = await _settingService.GetByKeyAsync("A4ReportShowCredits")     is AppSetting a6 ? a6.SettingValue != "0" : true;
                A4ReportShowCreditsList = await _settingService.GetByKeyAsync("A4ReportShowCreditsList") is AppSetting a7 ? a7.SettingValue == "1" : false;

                ReportShowSoldProducts   = await _settingService.GetByKeyAsync("ReportShowSoldProducts")   is AppSetting rs8 ? rs8.SettingValue == "1" : false;
                ReportShowCreditDetails  = await _settingService.GetByKeyAsync("ReportShowCreditDetails")  is AppSetting rs9 ? rs9.SettingValue == "1" : false;
                A4ReportShowSoldProducts  = await _settingService.GetByKeyAsync("A4ReportShowSoldProducts")  is AppSetting a8 ? a8.SettingValue == "1" : false;
                A4ReportShowCreditDetails = await _settingService.GetByKeyAsync("A4ReportShowCreditDetails") is AppSetting a9 ? a9.SettingValue == "1" : false;

                await LoadPrintersAsync();

                // load available templates from PrinterService
                try
                {
                    var svc = PrinterService.Instance;
                    AvailableTemplates.Clear();
                    foreach (var t in svc.GetAvailableTemplates()) AvailableTemplates.Add(t);
                    if (string.IsNullOrEmpty(SelectedTemplate) && AvailableTemplates.Count > 0) SelectedTemplate = AvailableTemplates.First();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to load templates: " + ex.ToString());
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Settings.LoadAsync failed: " + ex.ToString());
                // reset to safe defaults to keep UI responsive
                SelectedTemplate = "Simple";
            }
        }

        private async Task LoadPrintersAsync()
        {
            try
            {
                InstalledPrinters.Clear();
                var svc = PrinterService.Instance;
                var list = svc.GetInstalledPrinters()?.ToList() ?? new System.Collections.Generic.List<string>();
                foreach (var p in list) InstalledPrinters.Add(p);

                // if no printer selected, try default
                if (string.IsNullOrEmpty(ReceiptPrinter))
                {
                    var def = svc.GetDefaultPrinter();
                    if (!string.IsNullOrEmpty(def) && InstalledPrinters.Contains(def))
                        ReceiptPrinter = def;
                }
            }
            catch { }
        }

        /// <summary>
        /// معاينة قالب الفاتورة
        /// </summary>
        private void PreviewTemplate()
        {
            try
            {
                var testSale = CreateTestSale();

                if (testSale == null)
                {
                    MessageBox.Show(
                        "فشل إنشاء فاتورة تجريبية",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                var settings = PrinterService.Instance.LoadSettings();

                if (!string.IsNullOrEmpty(SelectedTemplate))
                    settings.TemplateName = SelectedTemplate;

                var isSimple = string.Equals(settings.TemplateName, "Simple", System.StringComparison.OrdinalIgnoreCase);

                Window previewWindow = isSimple
                    ? new Infrastructure.Printing.Views.SimpleTemplatePreviewWindow(testSale, settings)
                    : new Infrastructure.Printing.Views.ProfessionalTemplatePreviewWindow(testSale, settings);

                previewWindow.Owner = Application.Current.MainWindow;
                previewWindow.ShowDialog();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في معاينة القالب:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// طباعة فاتورة اختبار على الطابعة المحددة
        /// </summary>
        private void PrintTest()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ReceiptPrinter))
                {
                    MessageBox.Show("الرجاء اختيار طابعة أولاً.", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var testSale = CreateTestSale();
                if (testSale == null)
                {
                    MessageBox.Show("فشل إنشاء فاتورة تجريبية.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                PrinterService.Instance.PrintReceipt(testSale, ReceiptPrinter);

                MessageBox.Show("✅ تمت طباعة الفاتورة التجريبية بنجاح!", "نجحت الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"❌ فشلت الطباعة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// إنشاء فاتورة تجريبية للمعاينة
        /// </summary>
        private Sale CreateTestSale()
        {
            var sale = new Sale
            {
                Id = 12345,
                SaleNumber = "INV-2024-12345",
                SaleDate = System.DateTime.Now,
                CreatedAt = System.DateTime.Now,
                Cashier = "المستخدم التجريبي",
                RegisterId = "POS-001",
                TaxRate = TaxRate,
                Notes = "فاتورة تجريبية للمعاينة",
                CustomerId = null // زبون عام
            };

            // إضافة منتجات تجريبية
            sale.Items.Add(new SaleItem
            {
                ProductId = 1,
                Name = "منتج تجريبي 1",
                Quantity = 2,
                UnitPrice = 150.00m,
                UnitName = "قطعة",
                DiscountAmount = 0,
                DiscountPercent = 0
            });

            sale.Items.Add(new SaleItem
            {
                ProductId = 2,
                Name = "منتج تجريبي 2 بإسم طويل جداً",
                Quantity = 1,
                UnitPrice = 500.00m,
                UnitName = "علبة",
                DiscountAmount = 0,
                DiscountPercent = 0
            });

            sale.Items.Add(new SaleItem
            {
                ProductId = 3,
                Name = "منتج تجريبي 3",
                Quantity = 3.5m,
                UnitPrice = 75.50m,
                UnitName = "كيلو",
                DiscountAmount = 0,
                DiscountPercent = 0
            });

            // حساب المجاميع
            sale.CalculateTotals();
            sale.AmountPaid = sale.Total; // دفع كامل

            return sale;
        }

        /// <summary>
        /// فتح نافذة تخصيص القالب
        /// </summary>
        private void CustomizeTemplate()
        {
            try
            {
                Window? customizationWindow;
                var isSimple = string.Equals(SelectedTemplate, "Simple", System.StringComparison.OrdinalIgnoreCase);

                if (isSimple)
                {
                    customizationWindow = new Infrastructure.Printing.Views.SimpleTemplateWindow();
                }
                else
                {
                    customizationWindow = new Infrastructure.Printing.Views.ProfessionalTemplateWindow();
                }

                customizationWindow.Owner = Application.Current.MainWindow;
                var result = customizationWindow.ShowDialog();

                if (result == true)
                {
                    var settings = PrinterService.Instance.LoadSettings();
                    SelectedTemplate = settings.TemplateName;
                    ReceiptWidthMm = settings.ReceiptWidthMm;

                    MessageBox.Show(
                        "✅ تم حفظ إعدادات القالب بنجاح",
                        "تم بنجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في فتح نافذة التخصيص:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // existing simple fields kept for compatibility
        private string _connectionString = "Server=localhost\\SQLEXPRESS;Database=POS_BIGA;Trusted_Connection=True;TrustServerCertificate=True;";
        public string ConnectionString
        {
            get => _connectionString;
            set { _connectionString = value; OnPropertyChanged(nameof(ConnectionString)); }
        }

        private string _apiBaseUrl = string.Empty;
        public string ApiBaseUrl
        {
            get => _apiBaseUrl;
            set { _apiBaseUrl = value; OnPropertyChanged(nameof(ApiBaseUrl)); }
        }

        private string _selectedTheme = "Light";
        public string SelectedTheme { get => _selectedTheme; set { _selectedTheme = value; OnPropertyChanged(nameof(SelectedTheme)); } }

        private bool _enablePrinting;
        public bool EnablePrinting { get => _enablePrinting; set { _enablePrinting = value; OnPropertyChanged(nameof(EnablePrinting)); } }

        // Strongly-typed store settings
        private string _shopName;
        public string ShopName { get => _shopName; set { _shopName = value; OnPropertyChanged(nameof(ShopName)); } }

        private string _shopAddress;
        public string ShopAddress { get => _shopAddress; set { _shopAddress = value; OnPropertyChanged(nameof(ShopAddress)); } }

        private string _shopPhone;
        public string ShopPhone { get => _shopPhone; set { _shopPhone = value; OnPropertyChanged(nameof(ShopPhone)); } }

        private decimal _taxRate;
        public decimal TaxRate { get => _taxRate; set { _taxRate = value; OnPropertyChanged(nameof(TaxRate)); } }

        private string _currency = "MAD";
        public string Currency { get => _currency; set { _currency = value; OnPropertyChanged(nameof(Currency)); } }

        private bool _creditEnabled;
        public bool CreditEnabled { get => _creditEnabled; set { _creditEnabled = value; OnPropertyChanged(nameof(CreditEnabled)); } }

        private decimal _creditLimit;
        public decimal CreditLimit { get => _creditLimit; set { _creditLimit = value; OnPropertyChanged(nameof(CreditLimit)); } }

        private string _invoiceMessage;
        public string InvoiceMessage { get => _invoiceMessage; set { _invoiceMessage = value; OnPropertyChanged(nameof(InvoiceMessage)); } }

        // Printing related properties
        private string _receiptPrinter;
        public string ReceiptPrinter { get => _receiptPrinter; set { _receiptPrinter = value; OnPropertyChanged(nameof(ReceiptPrinter)); } }

        private int _receiptWidthMm = 80;
        public int ReceiptWidthMm { get => _receiptWidthMm; set { _receiptWidthMm = value; OnPropertyChanged(nameof(ReceiptWidthMm)); } }

        private bool _forceEscPos;
        public bool ForceEscPos { get => _forceEscPos; set { _forceEscPos = value; OnPropertyChanged(nameof(ForceEscPos)); } }

        private string _selectedTemplate;
        public string SelectedTemplate { get => _selectedTemplate; set { _selectedTemplate = value; OnPropertyChanged(nameof(SelectedTemplate)); } }

        private bool _autoPrint;
        public bool AutoPrint { get => _autoPrint; set { _autoPrint = value; OnPropertyChanged(nameof(AutoPrint)); } }

        // ─── أقسام تقرير الطباعة الحرارية ───────────────────────────
        private bool _reportShowHeader      = true;
        private bool _reportShowSummary     = true;
        private bool _reportShowTopProducts = true;
        private bool _reportShowCategories  = true;
        private bool _reportShowDailySales  = true;
        private bool _reportShowCredits     = true;
        private bool _reportShowCreditsList = false;

        public bool ReportShowHeader      { get => _reportShowHeader;      set { _reportShowHeader      = value; OnPropertyChanged(nameof(ReportShowHeader));      } }
        public bool ReportShowSummary     { get => _reportShowSummary;     set { _reportShowSummary     = value; OnPropertyChanged(nameof(ReportShowSummary));     } }
        public bool ReportShowTopProducts { get => _reportShowTopProducts; set { _reportShowTopProducts = value; OnPropertyChanged(nameof(ReportShowTopProducts)); } }
        public bool ReportShowCategories  { get => _reportShowCategories;  set { _reportShowCategories  = value; OnPropertyChanged(nameof(ReportShowCategories));  } }
        public bool ReportShowDailySales  { get => _reportShowDailySales;  set { _reportShowDailySales  = value; OnPropertyChanged(nameof(ReportShowDailySales));  } }
        public bool ReportShowCredits     { get => _reportShowCredits;     set { _reportShowCredits     = value; OnPropertyChanged(nameof(ReportShowCredits));     } }
        public bool ReportShowCreditsList { get => _reportShowCreditsList; set { _reportShowCreditsList = value; OnPropertyChanged(nameof(ReportShowCreditsList)); } }

        // ─── أقسام تقرير A4/A5 ──────────────────────────────────────
        private bool _a4ReportShowHeader      = true;
        private bool _a4ReportShowSummary     = true;
        private bool _a4ReportShowTopProducts = true;
        private bool _a4ReportShowCategories  = true;
        private bool _a4ReportShowDailySales  = true;
        private bool _a4ReportShowCredits     = true;
        private bool _a4ReportShowCreditsList = false;

        public bool A4ReportShowHeader      { get => _a4ReportShowHeader;      set { _a4ReportShowHeader      = value; OnPropertyChanged(nameof(A4ReportShowHeader));      } }
        public bool A4ReportShowSummary     { get => _a4ReportShowSummary;     set { _a4ReportShowSummary     = value; OnPropertyChanged(nameof(A4ReportShowSummary));     } }
        public bool A4ReportShowTopProducts { get => _a4ReportShowTopProducts; set { _a4ReportShowTopProducts = value; OnPropertyChanged(nameof(A4ReportShowTopProducts)); } }
        public bool A4ReportShowCategories  { get => _a4ReportShowCategories;  set { _a4ReportShowCategories  = value; OnPropertyChanged(nameof(A4ReportShowCategories));  } }
        public bool A4ReportShowDailySales  { get => _a4ReportShowDailySales;  set { _a4ReportShowDailySales  = value; OnPropertyChanged(nameof(A4ReportShowDailySales));  } }
        public bool A4ReportShowCredits     { get => _a4ReportShowCredits;     set { _a4ReportShowCredits     = value; OnPropertyChanged(nameof(A4ReportShowCredits));     } }
        public bool A4ReportShowCreditsList { get => _a4ReportShowCreditsList; set { _a4ReportShowCreditsList = value; OnPropertyChanged(nameof(A4ReportShowCreditsList)); } }

        // ─── خصائص جديدة: المنتجات المباعة + تفاصيل زبائن الكريدي ──
        private bool _reportShowSoldProducts   = false;
        private bool _reportShowCreditDetails  = false;
        private bool _a4ReportShowSoldProducts  = false;
        private bool _a4ReportShowCreditDetails = false;

        public bool ReportShowSoldProducts   { get => _reportShowSoldProducts;   set { _reportShowSoldProducts   = value; OnPropertyChanged(nameof(ReportShowSoldProducts));   } }
        public bool ReportShowCreditDetails  { get => _reportShowCreditDetails;  set { _reportShowCreditDetails  = value; OnPropertyChanged(nameof(ReportShowCreditDetails));  } }
        public bool A4ReportShowSoldProducts  { get => _a4ReportShowSoldProducts;  set { _a4ReportShowSoldProducts  = value; OnPropertyChanged(nameof(A4ReportShowSoldProducts));  } }
        public bool A4ReportShowCreditDetails { get => _a4ReportShowCreditDetails; set { _a4ReportShowCreditDetails = value; OnPropertyChanged(nameof(A4ReportShowCreditDetails)); } }

        public ICommand SaveCommand { get; }

        private async Task SaveAsync(object obj)
        {
            try
            {
                // persist key/value connection string and receipt printer
                await _settingService.SetValueAsync("ConnectionString", ConnectionString);
                await _settingService.SetValueAsync("ReceiptPrinter", ReceiptPrinter);
                await _settingService.SetValueAsync("PrintTemplate", SelectedTemplate ?? "Simple");
                await _settingService.SetValueAsync("AutoPrint", AutoPrint ? "1" : "0");
                await _settingService.SetValueAsync("ReceiptWidthMm", ReceiptWidthMm.ToString());
                await _settingService.SetValueAsync("ForceEscPos", ForceEscPos ? "1" : "0");

                // ✅ حفظ API Base URL وتحديث الـ cache فوراً
                await _settingService.SetValueAsync("ApiBaseUrl", ApiBaseUrl ?? string.Empty, "رابط الـ API للتطبيق المحمول");
                POS_BIGA.Infrastructure.Helpers.ImageStorageHelper.SetBaseUrl(ApiBaseUrl);

                // حفظ إعدادات أقسام التقرير الحراري
                await _settingService.SetValueAsync("ReportShowHeader",      ReportShowHeader      ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowSummary",     ReportShowSummary     ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowTopProducts", ReportShowTopProducts ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowCategories",  ReportShowCategories  ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowDailySales",  ReportShowDailySales  ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowCredits",     ReportShowCredits     ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowCreditsList", ReportShowCreditsList ? "1" : "0");

                // حفظ إعدادات أقسام تقرير A4
                await _settingService.SetValueAsync("A4ReportShowHeader",      A4ReportShowHeader      ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowSummary",     A4ReportShowSummary     ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowTopProducts", A4ReportShowTopProducts ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowCategories",  A4ReportShowCategories  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowDailySales",  A4ReportShowDailySales  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowCredits",     A4ReportShowCredits     ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowCreditsList", A4ReportShowCreditsList ? "1" : "0");

                await _settingService.SetValueAsync("ReportShowSoldProducts",   ReportShowSoldProducts   ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowCreditDetails",  ReportShowCreditDetails  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowSoldProducts",  A4ReportShowSoldProducts  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowCreditDetails", A4ReportShowCreditDetails ? "1" : "0");

                await _settingService.SetValueAsync("ReportShowSoldProducts",   ReportShowSoldProducts   ? "1" : "0");
                await _settingService.SetValueAsync("ReportShowCreditDetails",  ReportShowCreditDetails  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowSoldProducts",  A4ReportShowSoldProducts  ? "1" : "0");
                await _settingService.SetValueAsync("A4ReportShowCreditDetails", A4ReportShowCreditDetails ? "1" : "0");

                // persist typed store settings as single row
                var store = await _settingService.GetStoreSettingsAsync() ?? new AppSetting();
                store.ShopName = ShopName;
                store.ShopAddress = ShopAddress;
                store.ShopPhone = ShopPhone;
                store.TaxRate = TaxRate;
                store.Currency = Currency;
                store.CreditEnabled = CreditEnabled;
                store.CreditLimit = CreditLimit;
                store.InvoiceMessage = InvoiceMessage;

                await _settingService.SaveStoreSettingsAsync(store);

                // ✅ CRITICAL: Refresh PrinterService cache immediately
                System.Diagnostics.Debug.WriteLine("🔄 Settings saved - refreshing PrinterService cache...");
                await PrinterService.Instance.RefreshSettingsAsync();
                System.Diagnostics.Debug.WriteLine("✅ PrinterService cache refreshed successfully");

                // ✅ إشعار النظام بتحديث إعدادات الطابعة
                var appState = App.SharedAppState;
                appState?.NotifyPrinterSettingsChanged();

                MessageBox.Show(" ✅ تم حفظ الإعدادات بنجاح!\nسيتم استخدام الطابعة المحددة مباشرة.", 
                    "نجح الحفظ ", MessageBoxButton.OK, MessageBoxImage.Information);

                if (obj is Window w)
                {
                    w.DialogResult = true;
                    w.Close();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في حفظ الإعدادات: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack trace: {ex.StackTrace}");
                MessageBox.Show($"خطأ أثناء حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

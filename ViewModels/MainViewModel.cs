using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;
using POS_BIGA.Views.Pages;
using POS_BIGA.Views.Windows;
using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Infrastructure.Helpers;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;

namespace POS_BIGA.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private enum SalesDisplayMode
        {
            Categories,
            AllProducts,
            WithoutBarcode
        }

        private readonly List<Product> _allProducts = new();
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IAppStateService _appState;

        // ✅ Pagination support
        private const int ProductsPageSize = 50;
        private bool _isPaginationEnabled = false;

        public ObservableCollection<Product> Products { get; } = new ObservableCollection<Product>();
        public ObservableCollection<CartItem> CartItems { get; } = new ObservableCollection<CartItem>();
        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
        public ObservableCollection<Category> FilterCategories { get; } = new ObservableCollection<Category>();
        public ObservableCollection<object> LeftItems { get; } = new ObservableCollection<object>();

        // ✨ Event لطلب التركيز على خانة الباركود
        public event Action RequestBarcodeFocus;
        public event Action<Models.CartItem> CartItemAdded;

        private object _leftContent;
        public object LeftContent
        {
            get => _leftContent;
            set { _leftContent = value; OnPropertyChanged(nameof(LeftContent)); }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
                if (_isLoaded) _ = DebouncedSearchAsync();

                if (!string.IsNullOrEmpty(value))
                {
                    // كتابة جديدة → إيقاف عدّ الفئات + إعادة عدّ تحديد البحث
                    _returnToCategoriesTimer?.Stop();
                    _searchSelectTimer?.Stop();
                    _searchSelectTimer?.Start();
                }
                else
                {
                    // مُسح (اختيار منتج أو إضافة) → تحديد النص فوراً + إيقاف العدّ
                    _searchSelectTimer?.Stop();
                    RequestSearchSelectAll?.Invoke();
                }
            }
        }

        private Category _selectedCategoryFilter;
        public Category SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (_selectedCategoryFilter == value) return;
                _selectedCategoryFilter = value;
                OnPropertyChanged(nameof(SelectedCategoryFilter));
                if (_isLoaded) _ = DebouncedSearchAsync();
            }
        }

        private bool _isAdvancedSearchVisible;
        public bool IsAdvancedSearchVisible
        {
            get => _isAdvancedSearchVisible;
            set { _isAdvancedSearchVisible = value; OnPropertyChanged(nameof(IsAdvancedSearchVisible)); if (_isLoaded) _ = DebouncedSearchAsync(); }
        }

        private string _minPriceText;
        public string MinPriceText
        {
            get => _minPriceText;
            set { _minPriceText = value; OnPropertyChanged(nameof(MinPriceText)); if (_isLoaded) _ = DebouncedSearchAsync(); }
        }

        private string _maxPriceText;
        public string MaxPriceText
        {
            get => _maxPriceText;
            set { _maxPriceText = value; OnPropertyChanged(nameof(MaxPriceText)); if (_isLoaded) _ = DebouncedSearchAsync(); }
        }

        private bool _inStockOnly;
        public bool InStockOnly
        {
            get => _inStockOnly;
            set { _inStockOnly = value; OnPropertyChanged(nameof(InStockOnly)); if (_isLoaded) _ = DebouncedSearchAsync(); }
        }

        private bool _isLoaded = false;
        private CancellationTokenSource _searchCts = new CancellationTokenSource();
        private DispatcherTimer _notifTimer;

        // ══ Timer للرجوع التلقائي لواجهة الفئات بعد إضافة منتج ══
        private DispatcherTimer _returnToCategoriesTimer;
        private static readonly TimeSpan ReturnToCategoriesDelay = TimeSpan.FromSeconds(15);

        // ══ Timer لتحديد نص خانة البحث بعد 8 ثوانٍ من عدم التفاعل ══
        private DispatcherTimer _searchSelectTimer;
        private static readonly TimeSpan SearchSelectDelay = TimeSpan.FromSeconds(8);

        /// <summary>يُطلق لتحديد كل نص خانة البحث (SelectAll) من الـ View</summary>
        public event Action RequestSearchSelectAll;

        private string _notificationMessage;
        public string NotificationMessage
        {
            get => _notificationMessage;
            set { _notificationMessage = value; OnPropertyChanged(nameof(NotificationMessage)); }
        }

        private bool _notificationVisible;
        public bool NotificationVisible
        {
            get => _notificationVisible;
            set { _notificationVisible = value; OnPropertyChanged(nameof(NotificationVisible)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private decimal _totalSum;
        public decimal TotalSum
        {
            get => _totalSum;
            set { if (_totalSum == value) return; _totalSum = value; OnPropertyChanged(nameof(TotalSum)); }
        }

        public ICommand AddToCartCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand ShowProductDetailsCommand { get; }

        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand ExportProductsExcelCommand { get; }
        public ICommand ImportProductsExcelCommand { get; }

        public ICommand ShowCategoryCommand { get; }
        public ICommand BackToCategoriesCommand { get; }
        public ICommand ShowByCategoriesCommand { get; }
        public ICommand ShowAllProductsModeCommand { get; }
        public ICommand ShowWithoutBarcodeModeCommand { get; }

        public ICommand SuspendCartCommand { get; }
        public ICommand ShowSuspendedCartsCommand { get; }
        public ICommand ResumeSuspendedCartCommand { get; }

        private Category _activeCategory; // ✨ تتبع الفئة المحددة حاليا
        
        private SalesDisplayMode _salesDisplayMode = SalesDisplayMode.Categories;
        public bool IsDisplayByCategories => _salesDisplayMode == SalesDisplayMode.Categories;
        public bool IsDisplayAllProducts => _salesDisplayMode == SalesDisplayMode.AllProducts;
        public bool IsDisplayWithoutBarcode => _salesDisplayMode == SalesDisplayMode.WithoutBarcode;

        private bool _showingCategories = true;
        public bool ShowingCategories
        {
            get => _showingCategories;
            set { _showingCategories = value; OnPropertyChanged(nameof(ShowingCategories)); OnPropertyChanged(nameof(CurrentViewTitle)); OnPropertyChanged(nameof(ShowBackVisibility)); }
        }

        private string _currentCategoryName = string.Empty;
        public string CurrentViewTitle => ShowingCategories ? "الفئات" : (!string.IsNullOrEmpty(_currentCategoryName) ? _currentCategoryName : "منتجات");
        public Visibility ShowBackVisibility => ShowingCategories ? Visibility.Collapsed : Visibility.Visible;

        private bool _showingSuspendedCarts;
        public bool ShowingSuspendedCarts
        {
            get => _showingSuspendedCarts;
            set
            {
                _showingSuspendedCarts = value;
                OnPropertyChanged(nameof(ShowingSuspendedCarts));
                OnPropertyChanged(nameof(ShowingCart));
            }
        }

        public bool ShowingCart => !_showingSuspendedCarts;

        public ObservableCollection<SuspendedCart> SuspendedCarts { get; } = new ObservableCollection<SuspendedCart>();

        // ======================================
        // 🔒 صلاحيات المستخدم الحالي
        // ======================================

        public string CurrentUserName => SessionService.CurrentUser?.FullName ?? "غير معروف";
        public string CurrentUserRole => SessionService.CurrentUser?.RoleDisplayName ?? string.Empty;

        /// <summary>عنوان API للهاتف - يُجلب تلقائياً من IP الشبكة</summary>
        public string ApiUrl { get; } = GetLocalApiUrl();

        private static string GetLocalApiUrl()
        {
            try
            {
                // أسماء الشبكات الوهمية التي يجب تجاهلها
                var virtualAdapters = new[]
                {
                    "vmware", "vmnet", "virtual", "vethernet",
                    "bluetooth", "loopback", "hotspot", "vpn",
                    "tunne", "wintun", "hyper-v", "vbox"
                };

                var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

                // ✅ أولوية: Wi-Fi الحقيقي
                var ip = FindIpFromInterfaces(interfaces, virtualAdapters,
                    System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211);

                // ✅ ثانياً: Ethernet الحقيقي
                ip ??= FindIpFromInterfaces(interfaces, virtualAdapters,
                    System.Net.NetworkInformation.NetworkInterfaceType.Ethernet);

                // ✅ أي شبكة حقيقية متاحة
                ip ??= interfaces
                    .Where(n =>
                        n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        !virtualAdapters.Any(v => n.Description.ToLower().Contains(v) ||
                                                  n.Name.ToLower().Contains(v)))
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a =>
                        a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .FirstOrDefault();

                return ip != null ? $"http://{ip}:5000" : "http://localhost:5000";
            }
            catch
            {
                return "http://localhost:5000";
            }
        }

        private static string? FindIpFromInterfaces(
            System.Net.NetworkInformation.NetworkInterface[] interfaces,
            string[] virtualAdapters,
            System.Net.NetworkInformation.NetworkInterfaceType type)
        {
            return interfaces
                .Where(n =>
                    n.NetworkInterfaceType == type &&
                    n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    !virtualAdapters.Any(v => n.Description.ToLower().Contains(v) ||
                                              n.Name.ToLower().Contains(v)))
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a =>
                    a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !System.Net.IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString())
                .FirstOrDefault();
        }

        // 🛒 المبيعات
        public bool CanSell                 => SessionService.HasPermission(Permission.CanSell);
        public bool CanApplyDiscount        => SessionService.HasPermission(Permission.CanApplyDiscount);
        public bool CanEditCartPrice        => SessionService.HasPermission(Permission.CanEditCartPrice);
        public bool CanSuspendCart          => SessionService.HasPermission(Permission.CanSuspendCart);
        public bool CanViewSalesHistory     => SessionService.HasPermission(Permission.CanViewSalesHistory);

        // 📦 المنتجات
        public bool CanAddProduct           => SessionService.HasPermission(Permission.CanAddProduct);
        public bool CanEditProduct          => SessionService.HasPermission(Permission.CanEditProduct);
        public bool CanDeleteProduct        => SessionService.HasPermission(Permission.CanDeleteProduct);
        public bool CanImportExportProducts => SessionService.HasPermission(Permission.CanImportExportProducts);

        // 📊 التقارير
        public bool CanViewReports          => SessionService.HasPermission(Permission.CanViewReports);

        // ⚙️ الإعدادات والمستخدمين
        public bool CanAccessSettings       => SessionService.HasPermission(Permission.CanAccessSettings);
        public bool CanManageUsers          => SessionService.HasPermission(Permission.CanManageUsers);

        // 📋 المخزون
        public bool CanViewInventory        => SessionService.HasPermission(Permission.CanViewInventory);
        public bool CanManageInventory      => SessionService.HasPermission(Permission.CanManageInventory);

        // 👥 العملاء
        public bool CanViewCustomers        => SessionService.HasPermission(Permission.CanViewCustomers);
        public bool CanManageCredits        => SessionService.HasPermission(Permission.CanManageCredits);

        // ══ إظهار/إخفاء عمود نوع السعر في سلة المشتريات ══
        private bool _isPriceTypeColumnVisible = false;
        public bool IsPriceTypeColumnVisible
        {
            get => _isPriceTypeColumnVisible;
            set
            {
                if (_isPriceTypeColumnVisible == value) return;
                _isPriceTypeColumnVisible = value;
                OnPropertyChanged(nameof(IsPriceTypeColumnVisible));
            }
        }

        public ICommand TogglePriceTypeColumnCommand { get; private set; }

        // أمر تسجيل الخروج
        public ICommand LogoutCommand { get; private set; }

        // ======================================
        // 🔄 تحديث الصلاحيات عند تغييرها
        // ======================================
        private void RefreshPermissions()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CanSell));
                OnPropertyChanged(nameof(CanApplyDiscount));
                OnPropertyChanged(nameof(CanEditCartPrice));
                OnPropertyChanged(nameof(CanSuspendCart));
                OnPropertyChanged(nameof(CanViewSalesHistory));
                OnPropertyChanged(nameof(CanAddProduct));
                OnPropertyChanged(nameof(CanEditProduct));
                OnPropertyChanged(nameof(CanDeleteProduct));
                OnPropertyChanged(nameof(CanImportExportProducts));
                OnPropertyChanged(nameof(CanViewReports));
                OnPropertyChanged(nameof(CanAccessSettings));
                OnPropertyChanged(nameof(CanManageUsers));
                OnPropertyChanged(nameof(CanViewInventory));
                OnPropertyChanged(nameof(CanManageInventory));
                OnPropertyChanged(nameof(CanViewCustomers));
                OnPropertyChanged(nameof(CanManageCredits));
            });
        }

        public MainViewModel() : this(
            new ProductService(),
            new CategoryService(new POS_BIGA.Data.Repositories.CategoryRepository(), App.SharedAppState),
            App.SharedAppState
        ) 
        {
            System.Diagnostics.Debug.WriteLine("✅ MainViewModel: استخدام AppStateService المشترك");
        }

        public CheckoutViewModel CheckoutVM { get; private set; }

        public MainViewModel(IProductService productService, ICategoryService categoryService, IAppStateService appState)
        {
            _productService = productService;
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _appState = appState ?? throw new ArgumentNullException(nameof(appState));

            // ✅ الاشتراك في حدث تغيير الصلاحيات لتحديث الواجهة فوراً
            SessionService.PermissionsChanged += (_, _) => RefreshPermissions();

            AddToCartCommand = new RelayCommand(AddToCart);
            SearchCommand = new RelayCommand(async _ => await ExecuteSearchImmediateAsync());
            CloseCommand = new RelayCommand(obj => CloseWindow(obj));
            CheckoutCommand = new RelayCommand(_ => Checkout());

            // ✅ أمر تسجيل الخروج
            LogoutCommand = new RelayCommand(_ =>
            {
                var result = MessageBox.Show("هل تريد تسجيل الخروج؟", "تسجيل الخروج",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                SessionService.Logout();

                // تبديل ShutdownMode قبل فتح LoginWindow
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var loginWindow = new LoginWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();

                // إغلاق النافذة الرئيسية بعد إظهار LoginWindow
                foreach (Window w in Application.Current.Windows.OfType<MainWindow>().ToList())
                    w.Close();
            });

            var customerService = new CustomerService(new POS_BIGA.Data.Repositories.CustomerRepository(), _appState);
            var creditService = new CreditService(new POS_BIGA.Data.Repositories.CreditRepository(), _appState, customerService);
            var saleService = new SaleService(_appState);
            var printerService = PrinterService.Instance;

            // create CheckoutViewModel with printer service for automatic printing
            CheckoutVM = new CheckoutViewModel(CartItems, saleService, creditService, customerService, _appState, printerService);

            AttachCartHandlers();

            ShowProductDetailsCommand = new RelayCommand(p => ShowProductDetails(p as Product));

            AddProductCommand = new RelayCommand(_ => AddProduct());
            EditProductCommand = new RelayCommand(p => EditProduct(p as Product));
            DeleteProductCommand = new RelayCommand(async p => await DeleteProduct(p as Product));
            ExportProductsExcelCommand = new RelayCommand(_ => ExportProductsToExcel());
            ImportProductsExcelCommand = new RelayCommand(async _ => await ImportProductsFromExcelAsync());

            ShowCategoryCommand = new RelayCommand(c => ShowCategory(c as Category));
            BackToCategoriesCommand = new RelayCommand(_ => ShowCategories());
            ShowByCategoriesCommand = new RelayCommand(_ => SetSalesDisplayModeCategories());
            ShowAllProductsModeCommand = new RelayCommand(_ => SetSalesDisplayModeAllProducts());
            ShowWithoutBarcodeModeCommand = new RelayCommand(_ => SetSalesDisplayModeWithoutBarcode());

            SuspendCartCommand = new RelayCommand(_ => SuspendCart());
            ShowSuspendedCartsCommand = new RelayCommand(_ => ShowSuspendedCarts());
            ResumeSuspendedCartCommand = new RelayCommand(obj => ResumeSuspendedCart(obj as SuspendedCart));

            TogglePriceTypeColumnCommand = new RelayCommand(_ =>
                IsPriceTypeColumnVisible = !IsPriceTypeColumnVisible);

            if (_appState != null)
            {
                _appState.CategoriesChanged += async (s, e) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadCategoriesAsync();
                        ShowCategories();
                    });
                };

                _appState.ProductsChanged += async (s, e) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadProductsAsync();
                        if (string.IsNullOrWhiteSpace(SearchQuery))
                            ApplySalesDisplayMode();
                        else
                            await ExecuteSearchImmediateAsync();
                    });
                };

                _appState.UnitsChanged += async (s, e) =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadProductsAsync();
                    });
                };

                // ✅ الاستماع لتحديثات إعدادات الطباعة
                _appState.PrinterSettingsChanged += async (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("🖨️ MainViewModel: Printer settings changed event received");

                    // No need to recreate - Singleton handles it
                    // Just reload CheckoutVM to refresh its reference
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CheckoutVM = new CheckoutViewModel(CartItems, saleService, creditService, customerService, _appState, PrinterService.Instance);
                        OnPropertyChanged(nameof(CheckoutVM));
                        ShowNotification("✅ تم تحديث إعدادات الطابعة");
                    });
                };
            }

            _ = LoadDataAsync();

            _notifTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _notifTimer.Tick += (s, e) => { NotificationVisible = false; NotificationMessage = string.Empty; _notifTimer.Stop(); };

            _returnToCategoriesTimer = new DispatcherTimer { Interval = ReturnToCategoriesDelay };
            _returnToCategoriesTimer.Tick += (s, e) =>
            {
                _returnToCategoriesTimer.Stop();
                if (ShowingCategories) return;
                ShowCategories();
            };

            _searchSelectTimer = new DispatcherTimer { Interval = SearchSelectDelay };
            _searchSelectTimer.Tick += (s, e) =>
            {
                _searchSelectTimer.Stop();
                RequestSearchSelectAll?.Invoke();
            };

            // =======================================
            // ⏱️ Events تمهيدية مشروطة لأغراض تجريبية
            // =======================================

            // عند تغيير نص البحث، نحدد كل النص بعد 8 ثواني من عدم التفاعل
            // (تُستخدم EventRequestSearchSelectAll لطلب التركيز من الـ View)
            _searchSelectTimer = new DispatcherTimer { Interval = SearchSelectDelay };
            _searchSelectTimer.Tick += (s, e) =>
            {
                _searchSelectTimer.Stop();
                if (string.IsNullOrEmpty(SearchQuery)) return;
                RequestSearchSelectAll?.Invoke();
            };
        }

        private void AttachCartHandlers()
        {
            CartItems.CollectionChanged += CartItems_CollectionChanged;
            foreach (var item in CartItems)
                if (item is INotifyPropertyChanged npc) npc.PropertyChanged += CartItem_PropertyChanged;
            UpdateTotalSum();
        }

        private void DetachCartHandlers()
        {
            CartItems.CollectionChanged -= CartItems_CollectionChanged;
            foreach (var item in CartItems)
                if (item is INotifyPropertyChanged npc) npc.PropertyChanged -= CartItem_PropertyChanged;
        }

        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CartItem it in e.NewItems) if (it is INotifyPropertyChanged npc) npc.PropertyChanged += CartItem_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (CartItem it in e.OldItems) if (it is INotifyPropertyChanged npc) npc.PropertyChanged -= CartItem_PropertyChanged;
            }
            UpdateTotalSum();
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) ||
                e.PropertyName == nameof(CartItem.TotalPrice) ||
                e.PropertyName == nameof(CartItem.UnitPrice) ||
                e.PropertyName == nameof(CartItem.SelectedPriceType) ||
                e.PropertyName == nameof(CartItem.ActiveUnitPrice))
            {
                UpdateTotalSum();
            }
        }

        private async Task LoadDataAsync()
        {
            await LoadProductsAsync();
            await LoadCategoriesAsync();
            ShowCategories();
            _isLoaded = true;
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var list = await _categoryService.GetAllAsync();
                if (list == null || list.Count == 0)
                {
                    var defaults = new List<Category> { new Category { Name = "قرطاسية" }, new Category { Name = "مشروبات" }, new Category { Name = "مكتب" } };
                    foreach (var d in defaults) await _categoryService.AddAsync(d);
                    list = await _categoryService.GetAllAsync();
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var c in list) Categories.Add(c);

                    FilterCategories.Clear();
                    FilterCategories.Add(new Category { Id = 0, Name = "الكل" });
                    foreach (var c in list) FilterCategories.Add(c);

                    SelectedCategoryFilter = FilterCategories.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(SearchQuery))
                        ApplySalesDisplayMode();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل الفئات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                IsLoading = true; // ✅ Show loading indicator

                var products = await _productService.GetProductsAsync();

                _allProducts.Clear();
                _allProducts.AddRange(products);

                // try to populate missing ImageUrl values from local Images/Products directory
                try
                {
                    var imagesDir = ImageStorageHelper.GetBucketDirectoryPath("Products");
                    if (System.IO.Directory.Exists(imagesDir))
                    {
                        FillMissingImagesFromFolder(_allProducts, imagesDir);
                    }
                }
                catch { /* don't fail loading products if image scan fails */ }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Products.Clear();

                    var filteredAll = ApplyCurrentFilters(_allProducts);

                    // ✅ Pagination: Load first 50 products (faster initial load)
                    var productsToShow = _isPaginationEnabled
                        ? filteredAll.Take(ProductsPageSize)
                        : filteredAll;

                    foreach (var p in productsToShow)
                        Products.Add(p);

                    UpdateTotalSum();

                    // ✅ إشعار UI بتحديث Products
                    OnPropertyChanged(nameof(Products));

                    System.Diagnostics.Debug.WriteLine($"✅ Loaded {Products.Count} / {_allProducts.Count} products (filtered: {filteredAll.Count})");

                    if (string.IsNullOrWhiteSpace(SearchQuery))
                        ApplySalesDisplayMode();
                });
            }
            catch (Exception ex)
            {
                // swallow or log as appropriate; keep UI responsive
                MessageBox.Show($"خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false; // ✅ Hide loading indicator
            }
        }

        // Populate missing ImageUrl by matching files in imagesDir to product barcode, id or normalized name
        private void FillMissingImagesFromFolder(List<Product> products, string imagesDir)
        {
            var files = System.IO.Directory.GetFiles(imagesDir)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // create quick lookup by filename (without ext)
            var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(f);
                if (!lookup.ContainsKey(name)) lookup[name] = f;
            }

            foreach (var p in products)
            {
                if (!string.IsNullOrWhiteSpace(p.ImageUrl)) continue;

                // try barcode
                if (!string.IsNullOrWhiteSpace(p.Barcode) && lookup.TryGetValue(p.Barcode.Trim(), out var match))
                {
                    p.ImageUrl = new System.Uri(match).AbsoluteUri;
                    continue;
                }

                // try id
                var idKey = p.Id.ToString();
                if (lookup.TryGetValue(idKey, out match))
                {
                    p.ImageUrl = new System.Uri(match).AbsoluteUri;
                    continue;
                }

                // try normalized name tokens
                var nameTokens = (p.Name ?? string.Empty).Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
                bool assigned = false;
                foreach (var tok in nameTokens)
                {
                    if (lookup.TryGetValue(tok, out match))
                    {
                        p.ImageUrl = new System.Uri(match).AbsoluteUri;
                        assigned = true;
                        break;
                    }
                }
                if (assigned) continue;

                // last resort: try any file that contains barcode or name
                foreach (var kv in lookup)
                {
                    if (!string.IsNullOrWhiteSpace(p.Barcode) && kv.Key.IndexOf(p.Barcode.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        p.ImageUrl = new System.Uri(kv.Value).AbsoluteUri;
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(p.Name) && kv.Key.IndexOf(p.Name.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        p.ImageUrl = new System.Uri(kv.Value).AbsoluteUri;
                        break;
                    }
                }
            }
        }

        private async Task RefreshProductsAsync() => await LoadProductsAsync();

        private async Task DebouncedSearchAsync()
        {
            try { _searchCts.Cancel(); _searchCts.Dispose(); } catch { }
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            try
            {
                await Task.Delay(350, token);
                await ExecuteSearchAsync(token);
            }
            catch (TaskCanceledException) { }
        }

        private async Task ExecuteSearchImmediateAsync() => await ExecuteSearchAsync(CancellationToken.None);

        private async Task ExecuteSearchAsync(CancellationToken cancellationToken)
        {
            var raw   = (SearchQuery ?? string.Empty).Trim();
            var query = BarcodeHelper.Normalize(raw);

            if (string.IsNullOrEmpty(query))
            {
                // ✅ إذا كان هناك فلتر فئة مُحدَّد → نطبّقه على كل المنتجات
                if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != 0)
                {
                    var filtered = _allProducts
                        .Where(p => p.CategoryId == SelectedCategoryFilter.Id)
                        .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Products.Clear();
                        foreach (var p in filtered) Products.Add(p);
                        LeftItems.Clear();
                        foreach (var p in filtered) LeftItems.Add(p);
                        _currentCategoryName = SelectedCategoryFilter.Name;
                        ShowingCategories = false;
                        OnPropertyChanged(nameof(CurrentViewTitle));
                        OnPropertyChanged(nameof(LeftItems));
                    });
                }
                else
                {
                    // ✅ "الكل" مُحدَّد → عرض جميع المنتجات في الجدول + تطبيق وضع العرض
                    var all = IsAdvancedSearchVisible
                        ? ApplyCurrentFilters(_allProducts)
                        : _allProducts.ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Products.Clear();
                        foreach (var p in all) Products.Add(p);
                        OnPropertyChanged(nameof(Products));
                        ApplySalesDisplayMode();
                    });
                }
                return;
            }

            bool isCompleteScan = BarcodeHelper.IsCompleteScan(query);
            bool isDigits       = query.All(char.IsDigit);

            // ═══ كود بار كامل: بحث مباشر وإضافة للسلة ═══
            if (isCompleteScan)
            {
                // 1. بحث في الذاكرة أولاً (أسرع)
                var found = _allProducts.FirstOrDefault(
                    p => string.Equals(BarcodeHelper.Normalize(p.Barcode), query, StringComparison.OrdinalIgnoreCase));

                // 2. بحث في قاعدة البيانات إذا لم يوجد
                if (found == null)
                {
                    try { found = await _productService.GetProductByBarcodeAsync(query); } catch { }
                }

                if (cancellationToken.IsCancellationRequested) return;

                if (found != null)
                {
                    // ✅ منتج موجود → أضفه وأعِد التركيز فوراً
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddToCart(found, 1m);
                        SearchQuery = string.Empty;
                    });
                }
                else
                {
                    // ❌ كود بار غير موجود → مسح تلقائي + إشعار + استعداد للمنتج التالي
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SearchQuery = string.Empty;
                        ShowNotification($"⚠️ كود البار غير موجود: {query}");
                        RequestBarcodeFocus?.Invoke();
                    });
                }
                return;
            }

            // ═══ بحث نصي عادي (اسم أو كود بار جزئي) ══
            List<Product> results;
            if (isDigits && query.Length >= 3)
                results = _allProducts
                    .Where(p => !string.IsNullOrEmpty(p.Barcode)
                             && p.Barcode.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            else if (!isDigits && query.Length >= 2)
                results = _allProducts
                    .Where(p => !string.IsNullOrEmpty(p.Name)
                             && p.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
            else
                results = _allProducts
                    .Where(p => (!string.IsNullOrEmpty(p.Name)    && p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                             || (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(query)))
                    .ToList();

            if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != 0)
                results = results.Where(p => p.CategoryId == SelectedCategoryFilter.Id).ToList();

            if (IsAdvancedSearchVisible)
            {
                if (decimal.TryParse(MinPriceText, out var min)) results = results.Where(p => p.Price >= min).ToList();
                if (decimal.TryParse(MaxPriceText, out var max)) results = results.Where(p => p.Price <= max).ToList();
                if (InStockOnly) results = results.Where(p => p.StockQuantity > 0).ToList();
            }

            if (cancellationToken.IsCancellationRequested) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Products.Clear();  foreach (var p in results) Products.Add(p);
                LeftItems.Clear(); foreach (var p in results) LeftItems.Add(p);
                _currentCategoryName = "نتائج البحث";
                ShowingCategories = false;
                OnPropertyChanged(nameof(LeftItems));
                OnPropertyChanged(nameof(CurrentViewTitle));
            });
        }

        private void AddToCart(object parameter)
        {
            if (parameter is not Product product) return;
            AddToCart(product, 1m);
        }

        public void AddToCart(Product product, decimal quantity)
        {
            if (product == null || quantity <= 0m) return;
            var available = product.StockQuantity;
            if (available <= 0m)
            {
                ShowNotification($"تحذير: '{product.Name}' غير متوفر حالياً (الكمية = {available}). سيتم السماح بالبيع.");
            }

            var addQty = quantity;
            var existing = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
            var alreadyInCart = existing?.Quantity ?? 0m;

            if (available > 0m && alreadyInCart + addQty > available)
            {
                var remainingPossible = available - alreadyInCart;
                if (remainingPossible <= 0m)
                {
                    ShowNotification($"لا يمكن إضافة المزيد من '{product.Name}'. الكمية المتاحة: {available}.");
                    return;
                }

                // إضافة الكمية المتبقية مباشرة بدون سؤال
                addQty = remainingPossible;
            }

            if (existing != null)
            {
                existing.Quantity = Math.Round(existing.Quantity + addQty, 3);
                CartItemAdded?.Invoke(existing);
            }
            else
            {
                // respect currently selected global price type from CheckoutVM if available
                var selectedPriceType = CheckoutVM?.GlobalPriceType ?? POS_BIGA.Models.PriceType.Retail;

                // إدراج المنتج الجديد دائماً في السطر الأول ليكون الأحدث هو بالبداية (فوق القديم)
                var newItem = new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPrice = product.Price,
                    Quantity = Math.Round(addQty, 3),
                    Unit = product.UnitEntity?.Abbreviation ?? "pcs",
                    AllowCustomPrice = product.UnitEntity?.AllowCustomPrice ?? false,
                    WholesalePrice = product.WholesalePrice,
                    InstallmentPrice = product.InstallmentPrice,
                    SelectedPriceType = selectedPriceType
                };
                CartItems.Insert(0, newItem);
                CartItemAdded?.Invoke(newItem);
            }

            UpdateTotalSum();

            // ✨ مسح نص البحث وإرجاع التركيز لخانة الباركود بعد إضافة المنتج
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                SearchQuery = string.Empty; // يُطلق RequestSearchSelectAll تلقائياً
            }
            else
            {
                // البحث كان فارغاً (إضافة بالضغط) → تحديد البحث + بدء عدّ 8 ثوانٍ
                _searchSelectTimer?.Stop();
                _searchSelectTimer?.Start();
            }

            // ↩ إعادة تشغيل Timer للرجوع التلقائي للفئات (في جميع أوضاع عرض المنتجات)
            if (!ShowingCategories)
            {
                _returnToCategoriesTimer.Stop();
                _returnToCategoriesTimer.Start();
            }

            RequestBarcodeFocus?.Invoke();
        }

        private void ShowProductDetails(Product product)
        {
            if (product == null) return;
            var adminWindow = Application.Current?.MainWindow as Window;
            var editor = new ProductEditorWindow(product) { Owner = adminWindow };
            editor.ShowDialog();
        }

        private async void AddProduct()
        {
            var editor = new ProductEditorWindow();
            if (editor.ShowDialog() == true)
            {
                var newProduct = editor.Product;
                try
                {
                    await _productService.AddProductAsync(newProduct);
                    await RefreshProductsAsync();
                }
                catch (DuplicateProductException ex)
                {
                    MessageBox.Show(ex.Message, "تكرار منتج", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditProduct(Product product)
        {
            if (product == null) return;
            var editor = new ProductEditorWindow(product);
            if (editor.ShowDialog() == true)
            {
                var updated = editor.Product;
                try
                {
                    await _productService.UpdateProductAsync(updated);
                    await RefreshProductsAsync();
                    (_appState as AppStateService)?.NotifyProductsChanged();
                }
                catch (DuplicateProductException ex)
                {
                    MessageBox.Show(ex.Message, "تكرار منتج", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ أثناء تحديث المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteProduct(Product product)
        {
            if (product == null) return;
            if (MessageBox.Show($"هل أنت متأكد من حذف {product.Name}?", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var deleted = await _productService.DeleteProductAsync(product.Id);
                if (!deleted)
                {
                    MessageBox.Show(
                        "لا يمكن حذف هذا المنتج لأنه مرتبط بسجلات مبيعات موجودة.\nقم بحذف أو تعديل عناصر المبيعات أولاً.",
                        "تعذر الحذف",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                await RefreshProductsAsync();
            }
        }

        private void UpdateTotalSum() => TotalSum = CartItems.Sum(c => c.TotalPrice);
        public void ResetTotal() => TotalSum = 0m;

        private void CloseWindow(object parameter) { if (parameter is Window w) w.Close(); }
        private void Checkout() { if (CheckoutVM != null) { CheckoutVM.AmountPaid = 0m; } }

        private void ShowCategory(Category category)
        {
            if (category == null) return;
            _activeCategory = category;
            _salesDisplayMode = SalesDisplayMode.Categories; // Ensure mode is correct
            var prods = _allProducts.Where(p => p.CategoryId == category.Id).ToList();
            _returnToCategoriesTimer?.Stop();
            Application.Current.Dispatcher.Invoke(() =>
            {
                LeftItems.Clear(); foreach (var p in prods) LeftItems.Add(p);
                _currentCategoryName = category.Name; ShowingCategories = false; OnPropertyChanged(nameof(CurrentViewTitle)); OnPropertyChanged(nameof(LeftItems));
            });
        }

        public void ShowCategories()
        {
            _returnToCategoriesTimer.Stop();
            SetSalesDisplayModeCategories();
            LeftContent = null;
        }

        /// <summary>يوقف العدّ التنازلي للرجوع التلقائي للفئات</summary>
        public void StopReturnToCategoriesTimer() => _returnToCategoriesTimer?.Stop();

        /// <summary>يعيد العدّ التنازلي من الصفر (15 ثانية) — يُستدعى عند أي تفاعل مع واجهة المنتجات</summary>
        public void ResetReturnToCategoriesTimer()
        {
            // يعمل في جميع أوضاع عرض المنتجات (فئة، كل المنتجات، بدون باركود)
            if (!ShowingCategories)
            {
                _returnToCategoriesTimer?.Stop();
                _returnToCategoriesTimer?.Start();
            }
        }

        private void SetSalesDisplayModeCategories()
        {
            _salesDisplayMode = SalesDisplayMode.Categories;
            _activeCategory = null;
            ApplySalesDisplayMode();
        }

        private void SetSalesDisplayModeAllProducts()
        {
            _returnToCategoriesTimer.Stop();
            _salesDisplayMode = SalesDisplayMode.AllProducts;
            _activeCategory = null;
            ApplySalesDisplayMode();
            _returnToCategoriesTimer.Start();
        }

        private void SetSalesDisplayModeWithoutBarcode()
        {
            _returnToCategoriesTimer.Stop();
            _salesDisplayMode = SalesDisplayMode.WithoutBarcode;
            _activeCategory = null;
            ApplySalesDisplayMode();
            _returnToCategoriesTimer.Start();
        }

        private void ApplySalesDisplayMode()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LeftItems.Clear();

                if (_salesDisplayMode == SalesDisplayMode.Categories && _activeCategory == null)
                {
                    foreach (var c in Categories)
                        LeftItems.Add(c);

                    _currentCategoryName = string.Empty;
                    ShowingCategories = true;
                }
                else if (_salesDisplayMode == SalesDisplayMode.Categories && _activeCategory != null)
                {
                    var prods = _allProducts.Where(p => p.CategoryId == _activeCategory.Id).ToList();
                    foreach (var p in prods) LeftItems.Add(p);
                    
                    _currentCategoryName = _activeCategory.Name;
                    ShowingCategories = false;
                }
                else
                {
                    IEnumerable<Product> list = _allProducts;

                    if (_salesDisplayMode == SalesDisplayMode.WithoutBarcode)
                    {
                        list = list.Where(p => string.IsNullOrWhiteSpace(p.Barcode));
                        _currentCategoryName = "منتجات بدون باركود";
                    }
                    else
                    {
                        _currentCategoryName = "كل المنتجات";
                    }

                    foreach (var p in list.OrderBy(p => p.Name))
                        LeftItems.Add(p);

                    ShowingCategories = false;
                }

                OnPropertyChanged(nameof(IsDisplayByCategories));
                OnPropertyChanged(nameof(IsDisplayAllProducts));
                OnPropertyChanged(nameof(IsDisplayWithoutBarcode));
                OnPropertyChanged(nameof(CurrentViewTitle));
                OnPropertyChanged(nameof(LeftItems));
            });
        }

        private void ShowLeftContent(object content, string title = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LeftContent = content;
                if (!string.IsNullOrEmpty(title)) SetCurrentViewTitle(title);
                ShowingCategories = false;
            });
        }

        public void SetCurrentViewTitle(string title)
        {
            _currentCategoryName = title ?? string.Empty;
            OnPropertyChanged(nameof(CurrentViewTitle));
        }

        private void ShowNotification(string message)
        {
            NotificationMessage = message; NotificationVisible = true;
            try { _notifTimer.Stop(); _notifTimer.Start(); } catch { }
        }

        private void ExportProductsToExcel()
        {
            try
            {
                var saveDlg = new SaveFileDialog
                {
                    Title = "تصدير المنتجات إلى Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"products_[{Environment.UserName}]_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
                };

                if (saveDlg.ShowDialog() != true) return;

                // Export using currently applied filters — prefer filtered view over raw _allProducts
                var source = ApplyCurrentFilters(_allProducts);

                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Products");

                var headers = new[]
                {
                    "ID",
                    "اسم المنتج",
                    "الباركود",
                    "SKU",
                    "الفئة",
                    "سعر الشراء",
                    "سعر البيع",
                    "سعر الجملة",
                    "سعر التقسيط",
                    "المخزون الحالي",
                    "الحد الأدنى للمخزون",
                    "الوحدة",
                    "نشط",
                    "الوصف",
                    "مسار الصورة"
                };

                for (int c = 0; c < headers.Length; c++)
                    ws.Cell(1, c + 1).Value = headers[c];

                var headerRange = ws.Range(1, 1, 1, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var row = 2;
                foreach (var p in source)
                {
                    ws.Cell(row, 1).Value = p.Id;
                    ws.Cell(row, 2).Value = p.Name ?? string.Empty;
                    ws.Cell(row, 3).Value = p.Barcode ?? string.Empty;
                    ws.Cell(row, 4).Value = p.SKU ?? string.Empty;
                    ws.Cell(row, 5).Value = p.Category?.Name ?? string.Empty;
                    ws.Cell(row, 6).Value = p.PurchasePrice;
                    ws.Cell(row, 7).Value = p.SalePrice;
                    ws.Cell(row, 8).Value = p.WholesalePrice;
                    ws.Cell(row, 9).Value = p.InstallmentPrice;
                    ws.Cell(row, 10).Value = p.CurrentStock;
                    ws.Cell(row, 11).Value = p.MinimumStock;
                    ws.Cell(row, 12).Value = p.UnitEntity?.Abbreviation ?? string.Empty;
                    ws.Cell(row, 13).Value = p.IsActive;
                    ws.Cell(row, 14).Value = p.Description ?? string.Empty;
                    // ✅ عمود مسار الصورة — يُصدوَّر كما هو من DB
                    var imageUrl = p.ImageUrl ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(imageUrl) && !string.IsNullOrWhiteSpace(p.Name))
                        imageUrl = ImageStorageHelper.BuildDefaultImagePath(p.Name, "Products");
                    ws.Cell(row, 15).Value = imageUrl;
                    row++;
                }

                // تنسيق عمود مسار الصورة
                ws.Column(15).Style.Font.FontColor = XLColor.FromHtml("#1565C0");
                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(1);

                workbook.SaveAs(saveDlg.FileName);
                ShowNotification("✅ تم تصدير المنتجات إلى Excel بنجاح");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تصدير products: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ImportProductsFromExcelAsync()
        {
            try
            {
                var openDlg = new OpenFileDialog
                {
                    Title = "استيراد المنتجات من Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx"
                };

                if (openDlg.ShowDialog() != true) return;

                var existingProducts = await _productService.GetProductsAsync();
                var categories = await _categoryService.GetAllAsync();
                var categoryLookup = categories.ToDictionary(c => (c.Name ?? string.Empty).Trim(), c => c, StringComparer.OrdinalIgnoreCase);

                int added = 0, updated = 0, duplicateRows = 0, existingSkipped = 0, skippedInvalid = 0;
                bool? updateExistingProducts = null;
                var seenRows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var updatedDetails = new List<string>();

                using var workbook = new XLWorkbook(openDlg.FileName);
                var ws = workbook.Worksheet(1);
                if (ws == null)
                    throw new InvalidOperationException("الملف لا يحتوي على ورقة عمل صالحة.");

                var row = 2;
                while (true)
                {
                    var name = ws.Cell(row, 2).GetString().Trim();
                    var barcode = ws.Cell(row, 3).GetString().Trim();
                    var sku = ws.Cell(row, 4).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(sku))
                        break;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        skippedInvalid++;
                        row++;
                        continue;
                    }

                    var categoryName = ws.Cell(row, 5).GetString().Trim();
                    var purchasePrice = ParseDecimal(ws.Cell(row, 6).GetString());
                    var salePrice = ParseDecimal(ws.Cell(row, 7).GetString());
                    var wholesalePrice = ParseDecimal(ws.Cell(row, 8).GetString());
                    var installmentPrice = ParseDecimal(ws.Cell(row, 9).GetString());
                    var currentStock = ParseDecimal(ws.Cell(row, 10).GetString());
                    var minimumStock = ParseDecimal(ws.Cell(row, 11).GetString());
                    var isActive = ParseBool(ws.Cell(row, 13).GetString(), true);
                    var description = ws.Cell(row, 14).GetString();
                    // ✅ عمود مسار الصورة — اختياري، يُولَّد تلقائياً إن لم يكن موجوداً
                    var imageUrl = ws.Cell(row, 15).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(imageUrl))
                        imageUrl = ImageStorageHelper.BuildDefaultImagePath(name, "Products");

                    int categoryId;
                    if (!string.IsNullOrWhiteSpace(categoryName) && categoryLookup.TryGetValue(categoryName, out var foundCat))
                    {
                        categoryId = foundCat.Id;
                    }
                    else if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        var newCat = new Category { Name = categoryName, Description = string.Empty, IsActive = true, CreatedAt = DateTime.UtcNow };
                        await _categoryService.AddAsync(newCat);
                        categories = await _categoryService.GetAllAsync();
                        categoryLookup = categories.ToDictionary(c => (c.Name ?? string.Empty).Trim(), c => c, StringComparer.OrdinalIgnoreCase);
                        categoryId = categoryLookup[categoryName].Id;
                    }
                    else
                    {
                        categoryId = categories.FirstOrDefault()?.Id ?? 1;
                    }

                    var rowDuplicateKey = $"{name.Trim().ToLowerInvariant()}|{barcode.Trim().ToLowerInvariant()}|{sku.Trim().ToLowerInvariant()}|{categoryId}";
                    if (!seenRows.Add(rowDuplicateKey))
                    {
                        duplicateRows++;
                        row++;
                        continue;
                    }

                    var existing = existingProducts.FirstOrDefault(p =>
                        (!string.IsNullOrWhiteSpace(barcode) && string.Equals((p.Barcode ?? string.Empty).Trim(), barcode, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(sku) && string.Equals((p.SKU ?? string.Empty).Trim(), sku, StringComparison.OrdinalIgnoreCase)) ||
                        (string.Equals((p.Name ?? string.Empty).Trim(), name, StringComparison.OrdinalIgnoreCase) && p.CategoryId == categoryId));

                    if (existing != null)
                    {
                        if (updateExistingProducts == null)
                        {
                            var result = MessageBox.Show(
                                "تم العثور على منتجات موجودة مسبقاً.\nهل تريد تحديث المنتجات الموجودة (مثل الثمن والمخزون)؟\n\nنعم = تحديث كل الموجود\nلا = تخطي كل الموجود\nإلغاء = إيقاف الاستيراد",
                                "منتجات موجودة مسبقاً",
                                MessageBoxButton.YesNoCancel,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Cancel)
                                break;

                            updateExistingProducts = result == MessageBoxResult.Yes;
                        }

                        if (updateExistingProducts == false)
                        {
                            existingSkipped++;
                            row++;
                            continue;
                        }

                        var changes = new List<string>();
                        if (!string.Equals(existing.Name ?? string.Empty, name, StringComparison.Ordinal)) changes.Add("الاسم");
                        if (!string.Equals((existing.Barcode ?? string.Empty).Trim(), barcode, StringComparison.OrdinalIgnoreCase)) changes.Add("الباركود");
                        if (!string.Equals((existing.SKU ?? string.Empty).Trim(), sku, StringComparison.OrdinalIgnoreCase)) changes.Add("SKU");
                        if (existing.CategoryId != categoryId) changes.Add("الفئة");
                        if (existing.PurchasePrice != purchasePrice) changes.Add("ثمن الشراء");
                        if (existing.SalePrice != salePrice) changes.Add("ثمن البيع");
                        if (existing.WholesalePrice != wholesalePrice) changes.Add("سعر الجملة");
                        if (existing.InstallmentPrice != installmentPrice) changes.Add("سعر التقسيط");
                        if (existing.CurrentStock != currentStock) changes.Add("المخزون الحالي");
                        if (existing.MinimumStock != minimumStock) changes.Add("الحد الأدنى للمخزون");
                        if (!string.Equals(existing.Description ?? string.Empty, description ?? string.Empty, StringComparison.Ordinal)) changes.Add("الوصف");
                        if (existing.IsActive != isActive) changes.Add("الحالة");
                        if (!string.IsNullOrWhiteSpace(imageUrl) && !string.Equals(existing.ImageUrl ?? string.Empty, imageUrl, StringComparison.OrdinalIgnoreCase)) changes.Add("مسار الصورة");

                        if (changes.Count == 0)
                        {
                            row++;
                            continue;
                        }

                        existing.Name = name;
                        existing.Barcode = barcode;
                        existing.SKU = sku;
                        existing.CategoryId = categoryId;
                        existing.PurchasePrice = purchasePrice;
                        existing.SalePrice = salePrice;
                        existing.WholesalePrice = wholesalePrice;
                        existing.InstallmentPrice = installmentPrice;
                        existing.CurrentStock = currentStock;
                        existing.MinimumStock = minimumStock;
                        existing.Description = description ?? string.Empty;
                        existing.IsActive = isActive;
                        // ✅ تحديث مسار الصورة فقط إذا كان موجوداً في الملف
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                            existing.ImageUrl = imageUrl;
                        await _productService.UpdateProductAsync(existing);
                        updated++;

                        var itemLabel = !string.IsNullOrWhiteSpace(existing.Barcode) ? existing.Barcode : existing.Name;
                        updatedDetails.Add($"• {itemLabel}: {string.Join("، ", changes)}");
                    }
                    else
                    {
                        var newProduct = new Product
                        {
                            Name = name,
                            Barcode = barcode,
                            SKU = sku,
                            CategoryId = categoryId,
                            PurchasePrice = purchasePrice,
                            SalePrice = salePrice <= 0 ? purchasePrice : salePrice,
                            WholesalePrice = wholesalePrice,
                            InstallmentPrice = installmentPrice,
                            CurrentStock = currentStock,
                            MinimumStock = minimumStock,
                            Description = description ?? string.Empty,
                            IsActive = isActive,
                            // ✅ مسار الصورة — من الملف أو مولَّد تلقائياً
                            ImageUrl = imageUrl,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = SessionService.CurrentUserDisplayName
                        };

                        await _productService.AddProductAsync(newProduct);
                        existingProducts.Add(newProduct);
                        added++;
                    }

                    row++;
                }

                await RefreshProductsAsync();

                var successCount = added + updated;
                ShowNotification($"✅ تم الاستيراد بنجاح: {successCount} | المكررة: {duplicateRows}");

                var summary = new StringBuilder();
                summary.AppendLine($"تم الاستيراد بنجاح: {successCount}");
                summary.AppendLine($"المنتجات المكررة داخل الملف: {duplicateRows}");
                summary.AppendLine($"منتجات موجودة مسبقاً (تم تخطيها): {existingSkipped}");
                summary.AppendLine($"منتجات جديدة: {added}");
                summary.AppendLine($"منتجات محدثة: {updated}");
                summary.AppendLine($"صفوف غير صالحة: {skippedInvalid}");

                if (updatedDetails.Count > 0)
                {
                    summary.AppendLine();
                    summary.AppendLine("تفاصيل المنتجات المحدثة:");
                    foreach (var detail in updatedDetails.Take(20))
                        summary.AppendLine(detail);

                    if (updatedDetails.Count > 20)
                        summary.AppendLine($"... و {updatedDetails.Count - 20} منتج محدث آخر.");
                }

                MessageBox.Show(summary.ToString(), "نتيجة الاستيراد", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء استيراد المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0m;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var c)) return c;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) return i;
            return 0m;
        }

        private static bool ParseBool(string? value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            if (bool.TryParse(value, out var b)) return b;

            var text = value.Trim();
            return text switch
            {
                "1" => true,
                "0" => false,
                "yes" => true,
                "no" => false,
                "true" => true,
                "false" => false,
                "نعم" => true,
                "لا" => false,
                _ => defaultValue
            };
        }

        private List<Product> ApplyCurrentFilters(IEnumerable<Product> source)
        {
            var list = source ?? Enumerable.Empty<Product>();

            // apply category filter
            if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != 0)
            {
                list = list.Where(p => p.CategoryId == SelectedCategoryFilter.Id);
            }

            // apply search query
            var query = (SearchQuery ?? string.Empty).Trim();
            query = BarcodeHelper.FixBarcodeText(query);
            if (!string.IsNullOrEmpty(query))
            {
                bool isDigits = query.All(char.IsDigit);
                if (isDigits && query.Length >= 3)
                    list = list.Where(p => !string.IsNullOrEmpty(p.Barcode) && p.Barcode.StartsWith(query, StringComparison.OrdinalIgnoreCase));
                else if (!isDigits && query.Length >= 2)
                    list = list.Where(p => !string.IsNullOrEmpty(p.Name) && p.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase));
                else
                    list = list.Where(p => p.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(query)));
            }

            // advanced filters
            if (IsAdvancedSearchVisible)
            {
                if (decimal.TryParse(MinPriceText, out var min)) list = list.Where(p => p.Price >= min);
                if (decimal.TryParse(MaxPriceText, out var max)) list = list.Where(p => p.Price <= max);
                if (InStockOnly) list = list.Where(p => p.StockQuantity > 0);
            }

            return list.ToList();
        }

        private void SuspendCart()
        {
            if (CartItems.Count == 0)
            {
                ShowNotification("السلة فارغة لا يمكن تعليقها");
                return;
            }

            var suspended = new POS_BIGA.Models.SuspendedCart
            {
                Label = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var it in CartItems)
            {
                suspended.Items.Add(new POS_BIGA.Models.SuspendedCartItem
                {
                    ProductId = it.ProductId,
                    Name = it.Name,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Unit = it.Unit
                });
            }

            // store in app state
            (_appState as POS_BIGA.Services.AppStateService)?.AddSuspendedCart(suspended);

            // clear current cart
            CartItems.Clear();
            UpdateTotalSum();
            ShowNotification("تم تعليق السلة بنجاح");
        }

        private void ShowSuspendedCarts()
        {
            var svc = _appState as POS_BIGA.Services.AppStateService;
            if (svc == null) return;
            
            // Toggle behavior
            if (ShowingSuspendedCarts)
            {
                ShowingSuspendedCarts = false;
                return;
            }
            
            var list = svc.ListSuspendedCarts();
            if (list.Count == 0)
            {
                ShowNotification("لا توجد سلات معلقة حالياً ");
                return;
            }
            // show inline in cart panel
            Application.Current.Dispatcher.Invoke(() =>
            {
                SuspendedCarts.Clear();
                foreach (var c in list) SuspendedCarts.Add(c);
                ShowingSuspendedCarts = true;
            });
        }

        private void ResumeSuspendedCart(SuspendedCart cart)
        {
            if (cart == null) return;
            var svc = _appState as POS_BIGA.Services.AppStateService;
            // load into current CartItems
            CartItems.Clear();
            foreach (var it in cart.Items)
            {
                CartItems.Add(new Models.CartItem
                {
                    ProductId = it.ProductId,
                    Name = it.Name,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Unit = it.Unit
                });
            }

            // remove from suspended list
            svc?.RemoveSuspendedCart(cart.Id);

            // hide suspended carts view and show cart
            ShowingSuspendedCarts = false;

            UpdateTotalSum();
            ShowNotification(" تم استرجاع السلة المعلقة");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

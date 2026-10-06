using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;
using System.Collections.Specialized;
using POS_BIGA.Infrastructure.Printing.Abstractions;

namespace POS_BIGA.ViewModels
{
    /// <summary>
    /// 🛒 ViewModel الخروج - Checkout
    /// محدث ليتوافق مع التصميم الجديد
    /// </summary>
    public class CheckoutViewModel : INotifyPropertyChanged
    {
        public static Array PaymentMethods => Enum.GetValues(typeof(PaymentMethod));

        private readonly ISaleService _saleService;
        private readonly ICreditService _creditService;
        private readonly ICustomerService _customerService;
        private readonly IAppStateService _appState;
        private readonly IPrinterService _printerService;

        public ObservableCollection<CartItem> CartItems { get; }
        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();

        private decimal _total;
        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
                OnPropertyChanged(nameof(Remaining));
            }
        }

        private bool _isCompleting;
        public bool IsCompleting
        {
            get => _isCompleting;
            private set
            {
                if (_isCompleting == value) return;
                _isCompleting = value;
                OnPropertyChanged(nameof(IsCompleting));
                (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private decimal _amountPaid;
        public decimal AmountPaid
        {
            get => _amountPaid;
            set
            {
                _amountPaid = value < 0m ? 0m : value;
                OnPropertyChanged(nameof(AmountPaid));
                OnPropertyChanged(nameof(Remaining));
                UpdateRemainingDisplay();
                (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private string _remainingText = string.Empty;
        public string RemainingText
        {
            get => _remainingText;
            private set
            {
                _remainingText = value;
                OnPropertyChanged(nameof(RemainingText));
            }
        }

        private Brush _remainingBrush = Brushes.Black;
        public Brush RemainingBrush
        {
            get => _remainingBrush;
            private set
            {
                _remainingBrush = value;
                OnPropertyChanged(nameof(RemainingBrush));
            }
        }

        public decimal Remaining => Math.Max(0m, Total - AmountPaid);

        private PaymentMethod _selectedMethod = PaymentMethod.Cash;
        public PaymentMethod SelectedMethod
        {
            get => _selectedMethod;
            set
            {
                _selectedMethod = value;
                OnPropertyChanged(nameof(SelectedMethod));
                (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged(nameof(SelectedCustomer));
                UpdateRemainingDisplay();
                (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        public ICommand CompleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenQuickCashCommand { get; }
        public ICommand ApplyPriceTypeToAllCommand { get; }

        private PriceType _globalPriceType = PriceType.Retail;
        public PriceType GlobalPriceType
        {
            get => _globalPriceType;
            set
            {
                if (_globalPriceType == value) return;
                _globalPriceType = value;
                OnPropertyChanged(nameof(GlobalPriceType));
                ApplyPriceTypeToAll();
            }
        }

        public event Action<bool>? RequestClose;
        public event Action? RequestQuickCash;
        public event Action? RequestQuickCashClose;

        public CheckoutViewModel(
            ObservableCollection<CartItem> cart,
            ISaleService saleService,
            ICreditService creditService,
            ICustomerService customerService,
            IAppStateService appState,
            IPrinterService printerService = null)
        {
            CartItems = cart ?? throw new ArgumentNullException(nameof(cart));
            _saleService = saleService ?? throw new ArgumentNullException(nameof(saleService));
            _creditService = creditService;
            _customerService = customerService;
            _appState = appState;
            _printerService = printerService;

            CompleteCommand = new RelayCommand(async _ => await CompleteAsync(), _ => CanComplete());
            CancelCommand = new RelayCommand(_ => Cancel());
            OpenQuickCashCommand = new RelayCommand(_ => RequestQuickCash?.Invoke());
            ApplyPriceTypeToAllCommand = new RelayCommand(_ => ApplyPriceTypeToAll());

            AmountPaid = 0m;

            LoadCustomersAsync();
            RecalculateTotal();
            AttachCartHandlers();

            // ✅ الاستماع لتحديثات الزبائن
            if (_appState != null)
            {
                _appState.CustomerUpdated += OnCustomerUpdated;
            }
        }

        /// <summary>
        /// معالج حدث تحديث الزبائن
        /// </summary>
        private void OnCustomerUpdated(Customer customer)
        {
            // إعادة تحميل قائمة الزبائن
            LoadCustomersAsync();
        }

        private void AttachCartHandlers()
        {
            if (CartItems == null) return;
            CartItems.CollectionChanged += CartItems_CollectionChanged;
            foreach (var item in CartItems)
            {
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged += CartItem_PropertyChanged;
            }
        }

        private void DetachCartHandlers()
        {
            if (CartItems == null) return;
            CartItems.CollectionChanged -= CartItems_CollectionChanged;
            foreach (var item in CartItems)
            {
                if (item is INotifyPropertyChanged npc)
                    npc.PropertyChanged -= CartItem_PropertyChanged;
            }
        }

        private void CartItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CartItem it in e.NewItems)
                {
                    if (it is INotifyPropertyChanged npc)
                        npc.PropertyChanged += CartItem_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (CartItem it in e.OldItems)
                {
                    if (it is INotifyPropertyChanged npc)
                        npc.PropertyChanged -= CartItem_PropertyChanged;
                }
            }
            RecalculateTotal();
            (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) ||
                e.PropertyName == nameof(CartItem.TotalPrice) ||
                e.PropertyName == nameof(CartItem.UnitPrice) ||
                e.PropertyName == nameof(CartItem.SelectedPriceType) ||
                e.PropertyName == nameof(CartItem.ActiveUnitPrice))
            {
                RecalculateTotal();
            }
        }

        private async void LoadCustomersAsync()
        {
            if (_customerService == null) return;
            try
            {
                var list = await _customerService.GetAllAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers.Clear();
                    foreach (var c in list) Customers.Add(c);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الزبائن: {ex.Message}");
            }
        }

        private void UpdateRemainingDisplay()
        {
            var diff = Total - AmountPaid;
            if (diff > 0m)
            {
                if (SelectedCustomer == null)
                {
                    RemainingText = $"⚠️ المتبقي: {diff:N2} د.م - يجب اختيار زبون!";
                    RemainingBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // red
                }
                else
                {
                    RemainingText = $"المتبقي: {diff:N2} د.م (دين على {SelectedCustomer.FullName})";
                    RemainingBrush = new SolidColorBrush(Color.FromRgb(234, 88, 12)); // orange
                }
            }
            else if (diff < 0m)
            {
                var change = Math.Abs(diff);
                RemainingText = $"الباقي للزبون: {change:N2} د.م";
                RemainingBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green
            }
            else
            {
                RemainingText = "تم الدفع بالكامل ✓";
                RemainingBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green
            }
        }

        public void RecalculateTotal()
        {
            Total = CartItems.Sum(i => i.TotalPrice);
            (CompleteCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>تطبيق نوع السعر المختار على جميع عناصر السلة</summary>
        private void ApplyPriceTypeToAll()
        {
            foreach (var item in CartItems)
                item.SelectedPriceType = GlobalPriceType;

            RecalculateTotal();
        }

        private bool CanComplete()
        {
            if (IsCompleting) return false;
            if (CartItems == null || CartItems.Count == 0) return false;
            if (CartItems.Any(i => i.Quantity <= 0m)) return false;

            // إذا كان هناك مبلغ متبقي، يجب اختيار زبون
            if (AmountPaid < Total && SelectedCustomer == null)
            {
                return false;
            }

            return true;
        }

        private async Task CompleteAsync()
        {
            if (IsCompleting) return;

            try
            {
                IsCompleting = true;

                // إغلاق نافذة الدفع السريع فور الضغط على إتمام البيع (بدون تأثير على إغلاق الواجهة الرئيسية)
                RequestQuickCashClose?.Invoke();

                // 1. التحقق من صحة البيانات
                if (AmountPaid < Total && SelectedCustomer == null)
                {
                    _appState?.NotifyPrintingStatus($"⚠️ يجب اختيار زبون — المتبقي: {(Total - AmountPaid):N2} د.م");
                    return;
                }

                // 2. إنشاء Sale مع SaleItems
                var sale = new Sale
                {
                    SaleDate = DateTime.UtcNow,
                    Cashier = SessionService.CurrentUserDisplayName,
                    RegisterId = Environment.MachineName,
                    TaxRate = 0m,
                    Notes = string.Empty
                };

                // 3. إضافة عناصر البيع
                foreach (var cartItem in CartItems)
                {
                    sale.Items.Add(new SaleItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Name,
                        UnitName = cartItem.Unit,
                        Quantity = cartItem.Quantity,
                        // استخدام السعر الفعلي المختار (تجزئة / جملة / تقسيط)
                        UnitPrice = cartItem.ActiveUnitPrice,
                        DiscountAmount = 0m,
                        DiscountPercent = 0m
                    });
                }

                var customerId = SelectedCustomer?.Id;
                var remaining = Total - AmountPaid;

                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"📝 CheckoutViewModel: بدء حفظ البيع");
                System.Diagnostics.Debug.WriteLine($"   الإجمالي: {Total:N2} | المدفوع: {AmountPaid:N2} | المتبقي: {remaining:N2}");
                System.Diagnostics.Debug.WriteLine($"   الزبون: {(customerId.HasValue ? $"{SelectedCustomer?.FullName} (#{customerId})" : "غير محدد")}");
                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");

                // 4. حفظ البيع (سيُنشئ Credit تلقائياً إذا كان هناك متبقي)
                var savedSale = await _saleService.CreateSaleAsync(sale, AmountPaid, customerId, SelectedMethod);

                System.Diagnostics.Debug.WriteLine($"✓ تم حفظ البيع #{savedSale.Id} - {savedSale.SaleNumber}");

                // 5. طباعة الفاتورة تلقائياً — فقط إذا كان AutoPrint مفعّلاً
                if (_printerService != null)
                {
                    var settings = _printerService.LoadSettings();
                    if (settings.AutoPrint)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"📄 بدء الطباعة التلقائية للفاتورة {savedSale.SaleNumber}...");
                            await _printerService.PrintReceiptAsync(savedSale);
                            System.Diagnostics.Debug.WriteLine($"✓ تمت الطباعة بنجاح");
                        }
                        catch (Exception printEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ فشلت الطباعة: {printEx.Message}");
                            // Continue even if printing fails - sale is already saved
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"🚫 الطباعة التلقائية مُعطَّلة — تخطي الطباعة");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ لا توجد خدمة طباعة - تخطي الطباعة");
                }

                // 6. تنظيف السلة
                CartItems.Clear();
                AmountPaid = 0m;
                SelectedCustomer = null;

                // 7. إشعار النظام
                _appState?.NotifyProductsChanged();
                _appState?.NotifyCreditsChanged();
                _appState?.NotifyCustomersChanged();

                // 8. إغلاق النافذة
                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في CompleteAsync: {ex.Message}");
                MessageBox.Show(
                    $"حدث خطأ أثناء حفظ البيع:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsCompleting = false;
            }
        }

        private void Cancel()
        {
            // تأكيد من المستخدم قبل تفريغ السلة
            var result = MessageBox.Show(
                "هل تريد إلغاء العملية وتفريغ السلة؟\n\nسيتم حذف جميع المنتجات من السلة.",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            RequestQuickCashClose?.Invoke();
            CartItems.Clear();
            AmountPaid = 0m;
            SelectedCustomer = null;
            SelectedMethod = PaymentMethod.Cash;
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// تنظيف الموارد - إلغاء الاشتراك في الأحداث
        /// </summary>
        public void Dispose()
        {
            DetachCartHandlers();

            if (_appState != null)
            {
                _appState.CustomerUpdated -= OnCustomerUpdated;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

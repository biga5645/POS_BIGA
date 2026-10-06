using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;
using System.Windows.Threading;
using System;
using System.Collections.Generic;

namespace POS_BIGA.ViewModels
{
    public class CustomerViewModel : INotifyPropertyChanged
    {
        private readonly ICustomerService _service;
        private readonly ICreditService _creditService;
        private readonly IAppStateService _appState;
        private readonly DispatcherTimer _searchTimer;
        private List<Customer> _allCustomers = new List<Customer>();

        public event Action RequestAddCustomer;
        public event Action<Customer> RequestShowCustomerDetails;

        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                if (_selectedCustomer != null)
                {
                    FullName = _selectedCustomer.FullName;
                    Phone = _selectedCustomer.Phone;
                    Email = _selectedCustomer.Email;
                    Address = _selectedCustomer.Address;
                    Notes = _selectedCustomer.Notes;
                    CreditLimit = _selectedCustomer.CreditLimit;

                    // update ui-only fields
                    _ = UpdateSelectedCustomerBalanceAsync();
                }
                else
                {
                    FullName = Phone = Email = Address = Notes = string.Empty;
                    CreditLimit = 0m;
                }
                OnPropertyChanged(nameof(SelectedCustomer));
            }
        }

        private decimal _selectedCustomerBalance;
        public decimal SelectedCustomerBalance
        {
            get => _selectedCustomerBalance;
            set { _selectedCustomerBalance = value; OnPropertyChanged(nameof(SelectedCustomerBalance)); }
        }

        private decimal _creditLimit;
        public decimal CreditLimit { get => _creditLimit; set { _creditLimit = value; OnPropertyChanged(nameof(CreditLimit)); } }

        private string _fullName;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(nameof(FullName)); ((RelayCommand)AddCommand).NotifyCanExecuteChanged(); } }

        private string _phone;
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(nameof(Phone)); } }

        private string _email;
        public string Email { get => _email; set { _email = value; OnPropertyChanged(nameof(Email)); } }

        private string _address;
        public string Address { get => _address; set { _address = value; OnPropertyChanged(nameof(Address)); } }

        private string _notes;
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }

        // Search and Loading properties
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private int _totalCustomersCount;
        public int TotalCustomersCount
        {
            get => _totalCustomersCount;
            set { _totalCustomersCount = value; OnPropertyChanged(nameof(TotalCustomersCount)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand ViewCreditsCommand { get; }
        public ICommand ViewCustomerDetailsCommand { get; }

        public CustomerViewModel(ICustomerService service, IAppStateService appState) : this(service, null, appState) { }

        public CustomerViewModel(ICustomerService service, ICreditService creditService, IAppStateService appState)
        {
            try
            {
                _service = service;
                _creditService = creditService;
                _appState = appState;

                System.Diagnostics.Debug.WriteLine("🔧 تهيئة CustomerViewModel...");

                // Initialize search timer
                _searchTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300)
                };
                _searchTimer.Tick += (s, e) =>
                {
                    try
                    {
                        _searchTimer.Stop();
                        ApplyFilter();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ خطأ في timer: {ex.Message}");
                    }
                };

                RefreshCommand = new RelayCommand(async _ => await LoadAsync());
                AddCommand = new RelayCommand(_ => RequestAddCustomer?.Invoke());
                EditCommand = new RelayCommand(async _ => await EditAsync(), _ => SelectedCustomer != null);
                DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedCustomer != null);
                ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
                EditCustomerCommand = new RelayCommand(async param => await EditCustomerAsync(param as Customer));
                ViewCreditsCommand = new RelayCommand(param => ViewCustomerCredits(param as Customer));
                ViewCustomerDetailsCommand = new RelayCommand(param => 
                {
                    if (param is Customer customer)
                        RequestShowCustomerDetails?.Invoke(customer);
                });

                if (_appState != null)
                {
                    _appState.CustomersChanged += async (s, e) => await Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
                    // refresh debts for all customers when credits change (on UI thread)
                    _appState.CreditsChanged += async (s, e) => await Application.Current.Dispatcher.InvokeAsync(async () => await UpdateAllCustomersDebtAsync());
                }

                System.Diagnostics.Debug.WriteLine("✅ تمت تهيئة CustomerViewModel بنجاح");

                _ = LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ فادح في تهيئة CustomerViewModel: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                MessageBox.Show($"خطأ في تهيئة صفحة العملاء:\n{ex.Message}\n\n{ex.StackTrace}", "خطأ فادح",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 تطبيق تصفية البحث: '{SearchText}'");

                if (_allCustomers == null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ _allCustomers is null, skipping filter");
                    return;
                }

                Customers.Clear();

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    foreach (var customer in _allCustomers)
                    {
                        Customers.Add(customer);
                    }
                }
                else
                {
                    var searchLower = SearchText.ToLower();
                    var filtered = _allCustomers.Where(c =>
                        c.FullName?.ToLower().Contains(searchLower) == true ||
                        c.Phone?.ToLower().Contains(searchLower) == true ||
                        c.Email?.ToLower().Contains(searchLower) == true ||
                        c.Address?.ToLower().Contains(searchLower) == true ||
                        c.Id.ToString().Contains(searchLower)
                    ).ToList();

                    foreach (var customer in filtered)
                    {
                        Customers.Add(customer);
                    }

                    System.Diagnostics.Debug.WriteLine($"   النتائج: {Customers.Count} من {_allCustomers.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التصفية: {ex.Message}");
            }
        }

        private async Task EditCustomerAsync(Customer customer)
        {
            if (customer == null) return;

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // إنشاء نسخة مؤقتة للتعديل
                    var tempCustomer = new Customer
                    {
                        Id = customer.Id,
                        FullName = customer.FullName,
                        Phone = customer.Phone,
                        Email = customer.Email,
                        Address = customer.Address,
                        CreditLimit = customer.CreditLimit,
                        Notes = customer.Notes,
                        CreatedAt = customer.CreatedAt,
                        CreatedBy = customer.CreatedBy
                    };

                    var win = new Views.Windows.CustomerEditorWindow(customer.Id);
                    win.Owner = Application.Current.MainWindow;
                    var result = win.ShowDialog();

                    if (result == true)
                    {
                        _ = LoadAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تعديل العميل: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"حدث خطأ أثناء تعديل العميل:\n{ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ViewCustomerCredits(Customer customer)
        {
            if (customer == null) return;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var detailsWindow = new Views.Windows.CustomerDetailsWindow(customer);
                    detailsWindow.Owner = Application.Current.MainWindow;
                    detailsWindow.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في عرض تفاصيل العميل: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"حدث خطأ أثناء عرض تفاصيل العميل:\n{ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task UpdateAllCustomersDebtAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"🔄 CustomerViewModel.UpdateAllCustomersDebtAsync: تحديث ديون جميع العملاء");
                System.Diagnostics.Debug.WriteLine($"   عدد العملاء: {_allCustomers.Count}");

                if (_creditService == null)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ CreditService غير متوفر");
                    return;
                }

                foreach (var cust in _allCustomers)
                {
                    var credits = await _creditService.GetByCustomerAsync(cust.Id);
                    var oldDebt = cust.CurrentDebt;
                    cust.CurrentDebt = credits.Where(x => x.Status == CreditStatus.Unpaid).Sum(x => x.Amount);
                    // LastCreditDate يُحسب تلقائياً من Credits

                    System.Diagnostics.Debug.WriteLine($"   ✓ {cust.FullName}: الديون القديمة={oldDebt:N2}, الديون الجديدة={cust.CurrentDebt:N2}, عدد الديون={credits.Count}");
                }

                // Refresh displayed customers
                ApplyFilter();

                System.Diagnostics.Debug.WriteLine($"✓ تم تحديث ديون جميع العملاء");
                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في UpdateAllCustomersDebtAsync: {ex}");
            }
        }

        private async Task UpdateSelectedCustomerBalanceAsync()
        {
            try
            {
                if (SelectedCustomer == null || _creditService == null)
                {
                    SelectedCustomerBalance = 0m;
                    return;
                }

                var credits = await _creditService.GetByCustomerAsync(SelectedCustomer.Id);
                var unpaid = credits.Where(c => c.Status == CreditStatus.Unpaid).Sum(c => c.Amount);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedCustomerBalance = unpaid;

                    // set ui-only properties on customer
                    SelectedCustomer.CurrentDebt = unpaid;
                    // LastCreditDate يُحسب تلقائياً من Credits
                });
            }
            catch
            {
                SelectedCustomerBalance = 0m;
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"📥 تحميل العملاء...");

                var list = await _service.GetAllAsync();
                _allCustomers = list.OrderBy(c => c.FullName).ToList();
                TotalCustomersCount = _allCustomers.Count;

                System.Diagnostics.Debug.WriteLine($"   إجمالي العملاء: {TotalCustomersCount}");

                // update debt info for all customers if credit service available
                if (_creditService != null)
                {
                    foreach (var cust in _allCustomers)
                    {
                        var credits = await _creditService.GetByCustomerAsync(cust.Id);
                        cust.CurrentDebt = credits.Where(x => x.Status == CreditStatus.Unpaid).Sum(x => x.Amount);
                        // LastCreditDate يُحسب تلقائياً من Credits
                    }
                }

                // Apply filter
                ApplyFilter();

                SelectedCustomer = Customers.FirstOrDefault();

                System.Diagnostics.Debug.WriteLine($"✅ تم تحميل {Customers.Count} عميل");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في تحميل العملاء: {ex.Message}");
                MessageBox.Show($"حدث خطأ أثناء تحميل الزبائن: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task AddAsync()
        {
            try
            {
                var c = new Customer { FullName = FullName?.Trim() ?? string.Empty, Phone = Phone ?? string.Empty, Email = Email ?? string.Empty, Address = Address ?? string.Empty, Notes = Notes ?? string.Empty, CreditLimit = CreditLimit };
                await _service.AddAsync(c);

                FullName = Phone = Email = Address = Notes = string.Empty;
                CreditLimit = 0m;
                // rely on AppStateService event to reload
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء إضافة الزبون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task EditAsync()
        {
            if (SelectedCustomer == null) return;
            try
            {
                SelectedCustomer.FullName = FullName?.Trim() ?? string.Empty;
                SelectedCustomer.Phone = Phone ?? string.Empty;
                SelectedCustomer.Email = Email ?? string.Empty;
                SelectedCustomer.Address = Address ?? string.Empty;
                SelectedCustomer.Notes = Notes ?? string.Empty;
                SelectedCustomer.CreditLimit = CreditLimit;

                await _service.UpdateAsync(SelectedCustomer);
                // rely on AppStateService event to reload
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تعديل الزبون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedCustomer == null) return;
            if (MessageBox.Show($"هل تريد حذف الزبون {SelectedCustomer.FullName}?", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.DeleteAsync(SelectedCustomer.Id);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

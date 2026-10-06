using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;

namespace POS_BIGA.ViewModels
{
    public class CreditViewModel : INotifyPropertyChanged
    {
        private readonly ICreditService _service;
        private readonly IAppStateService _appState;
        private readonly ICustomerService _customerService;

        public ObservableCollection<Credit> Credits { get; } = new ObservableCollection<Credit>();
        public ObservableCollection<Customer> Customers { get; } = new ObservableCollection<Customer>();

        public Array Statuses => Enum.GetValues(typeof(CreditStatus));

        // 📊 خصائص الإحصائيات
        public decimal UnpaidCreditsAmount => Credits.Where(c => c.Status == CreditStatus.Unpaid).Sum(c => c.Amount);
        public decimal PaidCreditsAmount => Credits.Where(c => c.Status == CreditStatus.Paid).Sum(c => c.Amount);

        private Credit _selectedCredit;
        public Credit SelectedCredit
        {
            get => _selectedCredit;
            set
            {
                _selectedCredit = value;
                if (_selectedCredit != null)
                {
                    SelectedCustomerId = _selectedCredit.CustomerId;
                    Amount = _selectedCredit.Amount;
                    Status = _selectedCredit.Status;
                }
                else
                {
                    SelectedCustomerId = 0;
                    Amount = 0m;
                    Status = CreditStatus.Unpaid;
                }
                OnPropertyChanged(nameof(SelectedCredit));
            }
        }

        private int _selectedCustomerId;
        public int SelectedCustomerId { get => _selectedCustomerId; set { _selectedCustomerId = value; OnPropertyChanged(nameof(SelectedCustomerId)); if (AddCommand is RelayCommand addCmd) addCmd.NotifyCanExecuteChanged(); } }

        private decimal _amount;
        public decimal Amount { get => _amount; set { _amount = value; OnPropertyChanged(nameof(Amount)); if (AddCommand is RelayCommand addCmd) addCmd.NotifyCanExecuteChanged(); } }

        private CreditStatus _status = CreditStatus.Unpaid;
        public CreditStatus Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MarkAsPaidCommand { get; }

        // ctor accepts optional ICustomerService for populating Customers list
        public CreditViewModel(ICreditService service, IAppStateService appState) : this(service, null, appState, null) { }

        public CreditViewModel(ICreditService service, ICustomerService customerService, IAppStateService appState) : this(service, customerService, appState, null) { }

        public CreditViewModel(ICreditService service, ICustomerService customerService, IAppStateService appState, int? initialSelectedCustomerId)
        {
            _service = service;
            _appState = appState;
            _customerService = customerService;

            // if an initial customer id is provided, set it so LoadCustomersAsync won't overwrite it
            if (initialSelectedCustomerId.HasValue)
            {
                SelectedCustomerId = initialSelectedCustomerId.Value;
            }

            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync(), _ => SelectedCustomerId > 0 && Amount > 0m);
            EditCommand = new RelayCommand(async _ => await EditAsync(), _ => SelectedCredit != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedCredit != null);
            MarkAsPaidCommand = new RelayCommand(async _ => await MarkAsPaidAsync(), _ => SelectedCredit != null && SelectedCredit.Status == CreditStatus.Unpaid);

            if (_appState != null)
            {
                _appState.CreditsChanged += async (s, e) => await LoadAsync();
                _appState.CustomersChanged += async (s, e) => await LoadCustomersAsync();
            }

            _ = LoadAsync();
            _ = LoadCustomersAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                Credits.Clear();
                var list = await _service.GetAllAsync();
                foreach (var c in list) Credits.Add(c);
                SelectedCredit = Credits.FirstOrDefault();

                // تحديث الإحصائيات
                OnPropertyChanged(nameof(UnpaidCreditsAmount));
                OnPropertyChanged(nameof(PaidCreditsAmount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل الديون: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                Customers.Clear();
                if (_customerService != null)
                {
                    var list = await _customerService.GetAllAsync();
                    foreach (var c in list) Customers.Add(c);
                    // keep SelectedCustomerId if possible
                    if (SelectedCustomerId == 0 && Customers.FirstOrDefault() is Customer first) SelectedCustomerId = first.Id;
                }
            }
            catch
            {
                // ignore
            }
        }

        public async Task AddAsync()
        {
            try
            {
                if (SelectedCustomerId <= 0)
                {
                    MessageBox.Show("الرجاء اختيار زبون صالح قبل إضافة الدين.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Amount <= 0m)
                {
                    MessageBox.Show("الرجاء إدخال مبلغ أكبر من صفر.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var credit = new Credit { CustomerId = SelectedCustomerId, Amount = Amount, Status = Status };

                await _service.AddAsync(credit);

                SelectedCustomerId = 0;
                Amount = 0m;
                Status = CreditStatus.Unpaid;
                // reload will be triggered by event
            }
            catch (System.Exception ex)
            {
                // show full details for debugging
                System.Diagnostics.Debug.WriteLine("Credit Add Error: " + ex.ToString());
                MessageBox.Show($"حدث خطأ أثناء إضافة الدين:\n{ex.Message}\n\nتفاصيل (لتشخيص):\n{ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task EditAsync()
        {
            if (SelectedCredit == null) return;
            try
            {
                SelectedCredit.CustomerId = SelectedCustomerId;
                SelectedCredit.Amount = Amount;
                SelectedCredit.Status = Status;
                await _service.UpdateAsync(SelectedCredit);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تعديل الدين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedCredit == null) return;
            if (MessageBox.Show($"هل تريد حذف الدين رقم {SelectedCredit.Id}?", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await _service.DeleteAsync(SelectedCredit.Id);
            }
        }

        public async Task MarkAsPaidAsync()
        {
            if (SelectedCredit == null) return;
            try
            {
                if (MessageBox.Show(
                    $"هل تريد تحديد الدين رقم {SelectedCredit.Id} كمدفوع؟\n\n" +
                    $"العميل: {SelectedCredit.Customer?.FullName ?? "غير معروف"}\n" +
                    $"المبلغ: {SelectedCredit.Amount:N2} د.م",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SelectedCredit.Status = CreditStatus.Paid;
                    await _service.UpdateAsync(SelectedCredit);
                    MessageBox.Show("✓ تم تحديد الدين كمدفوع بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحديد الدين كمدفوع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

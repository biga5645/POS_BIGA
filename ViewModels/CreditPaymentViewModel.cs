using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Commands;
using POS_BIGA.Data;
using POS_BIGA.Models;

namespace POS_BIGA.ViewModels
{
    public class CreditPaymentViewModel : INotifyPropertyChanged
    {
        private readonly int _customerId;
        private List<Credit> _unpaidCredits = new();

        public event Action<bool> RequestClose;
        public event Action RequestPrint;

        #region Properties

        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(nameof(CustomerName)); }
        }

        private int _unpaidInvoicesCount;
        public int UnpaidInvoicesCount
        {
            get => _unpaidInvoicesCount;
            set { _unpaidInvoicesCount = value; OnPropertyChanged(nameof(UnpaidInvoicesCount)); }
        }

        private decimal _totalDebt;
        public decimal TotalDebt
        {
            get => _totalDebt;
            set { _totalDebt = value; OnPropertyChanged(nameof(TotalDebt)); }
        }

        private decimal _amountToPay;
        public decimal AmountToPay
        {
            get => _amountToPay;
            set 
            { 
                _amountToPay = Math.Min(value, TotalDebt);
                OnPropertyChanged(nameof(AmountToPay)); 
                OnPropertyChanged(nameof(RemainingAfter));
            }
        }

        public decimal RemainingAfter => Math.Max(0, TotalDebt - AmountToPay);

        private string _notes;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public ObservableCollection<string> PaymentMethods { get; } = new()
        {
            "نقدي",
            "بطاقة",
            "شيك",
            "تحويل بنكي"
        };

        private string _selectedPaymentMethod = "نقدي";
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set { _selectedPaymentMethod = value; OnPropertyChanged(nameof(SelectedPaymentMethod)); }
        }

        #endregion

        #region Commands

        public ICommand PayFullCommand { get; }
        public ICommand PayHalfCommand { get; }
        public ICommand PayQuarterCommand { get; }
        public ICommand Pay100Command { get; }
        public ICommand Pay200Command { get; }
        public ICommand Pay500Command { get; }
        public ICommand PayAndPrintCommand { get; }
        public ICommand PayOnlyCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand PreviewReceiptCommand { get; }

        #endregion

        public CreditPaymentViewModel(int customerId, string customerName)
        {
            _customerId = customerId;
            CustomerName = customerName;

            PayFullCommand = new RelayCommand(_ => AmountToPay = TotalDebt);
            PayHalfCommand = new RelayCommand(_ => AmountToPay = TotalDebt / 2);
            PayQuarterCommand = new RelayCommand(_ => AmountToPay = TotalDebt / 4);
            Pay100Command = new RelayCommand(_ => AmountToPay = Math.Min(100, TotalDebt));
            Pay200Command = new RelayCommand(_ => AmountToPay = Math.Min(200, TotalDebt));
            Pay500Command = new RelayCommand(_ => AmountToPay = Math.Min(500, TotalDebt));
            PayAndPrintCommand = new RelayCommand(async _ => await PayAndPrintAsync());
            PayOnlyCommand = new RelayCommand(async _ => await PayOnlyAsync());
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
            PreviewReceiptCommand = new RelayCommand(_ => PreviewReceipt());

            LoadCustomerDebts();
        }

        private void PreviewReceipt()
        {
            try
            {
                var receipt = GenerateReceiptContent();
                MessageBox.Show(receipt, "معاينة الإيصال", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في المعاينة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadCustomerDebts()
        {
            try
            {
                using var ctx = new AppDbContext();

                _unpaidCredits = await ctx.Credits
                    .Where(c => c.CustomerId == _customerId && c.RemainingAmount > 0)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                if (_unpaidCredits.Count == 0)
                {
                    MessageBox.Show("✅ لا توجد ديون متبقية لهذا العميل!", "معلومة", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    RequestClose?.Invoke(false);
                    return;
                }

                UnpaidInvoicesCount = _unpaidCredits.Count;
                TotalDebt = _unpaidCredits.Sum(c => c.RemainingAmount);
                AmountToPay = TotalDebt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في تحميل بيانات الديون:\n{ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PayAndPrintAsync()
        {
            if (await ProcessPaymentAsync())
            {
                RequestPrint?.Invoke();
                RequestClose?.Invoke(true);
            }
        }

        private async Task PayOnlyAsync()
        {
            if (await ProcessPaymentAsync())
            {
                RequestClose?.Invoke(true);
            }
        }

        private async Task<bool> ProcessPaymentAsync()
        {
            try
            {
                if (AmountToPay <= 0)
                {
                    MessageBox.Show("⚠️ الرجاء إدخال مبلغ الدفع!", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (AmountToPay > TotalDebt)
                {
                    MessageBox.Show("⚠️ المبلغ المدفوع أكبر من إجمالي الديون!", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                using var ctx = new AppDbContext();

                decimal remainingToPay = AmountToPay;
                var paymentMethod = Enum.TryParse<PaymentMethod>(SelectedPaymentMethod, out var method) 
                    ? method 
                    : Models.PaymentMethod.Cash;

                var receivedBy = POS_BIGA.Services.SessionService.CurrentUserDisplayName;

                foreach (var credit in _unpaidCredits.OrderBy(c => c.CreatedAt))
                {
                    if (remainingToPay <= 0) break;

                    var credit_db = await ctx.Credits.FindAsync(credit.Id);
                    if (credit_db == null || credit_db.RemainingAmount <= 0) continue;

                    decimal amountForThisCredit = Math.Min(remainingToPay, credit_db.RemainingAmount);

                    var payment = new Payment
                    {
                        CreditId = credit_db.Id,
                        Amount = amountForThisCredit,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = paymentMethod,
                        ReceivedBy = receivedBy,
                        Notes = string.IsNullOrWhiteSpace(Notes) ? string.Empty : Notes,
                        CreatedAt = DateTime.Now
                    };

                    ctx.Payments.Add(payment);

                    credit_db.PaidAmount += amountForThisCredit;
                    credit_db.RemainingAmount -= amountForThisCredit;
                    credit_db.UpdatedAt = DateTime.Now;

                    if (credit_db.RemainingAmount <= 0)
                    {
                        credit_db.Status = CreditStatus.Paid;
                        credit_db.RemainingAmount = 0;
                    }
                    else if (credit_db.PaidAmount > 0)
                    {
                        credit_db.Status = CreditStatus.PartiallyPaid;
                    }

                    remainingToPay -= amountForThisCredit;
                }

                await ctx.SaveChangesAsync();

                var appState = App.SharedAppState;
                appState?.NotifyCreditsChanged();
                appState?.NotifyCustomersChanged();

                MessageBox.Show(
                    $"✅ تم تسجيل الدفع بنجاح!\n\n" +
                    $"المبلغ المدفوع: {AmountToPay:N2} د.م\n" +
                    $"المتبقي: {RemainingAfter:N2} د.م\n" +
                    $"تم توزيع الدفع على {_unpaidCredits.Count} فاتورة",
                    "نجح الدفع",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                MessageBox.Show(
                    $"❌ خطأ في قاعدة البيانات:\n{innerMessage}\n\n" +
                    $"تفاصيل: {dbEx.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show(
                    $"❌ خطأ في معالجة الدفع:\n{innerMessage}\n\n" +
                    $"تفاصيل: {ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public string GenerateReceiptContent()
        {
            // استخدام عرض 42 حرفاً لضمان التوافق مع معظم الطابعات الحرارية (58mm و 80mm)
            var separator = new string('-', 42);
            var doubleSeparator = new string('=', 42);
            string receiptId = $"RCP-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

            // تحسين محاذاة العناوين والقيم
            return $@"
      {doubleSeparator}
      إيصال تسديد ديون
      PAYMENT RECEIPT
      {doubleSeparator}

رقم الإيصال: {receiptId}
التاريخ: {DateTime.Now:yyyy-MM-dd} | {DateTime.Now:HH:mm}
{separator}

العميل: {CustomerName}
{(string.IsNullOrWhiteSpace(Notes) ? "" : $"ملاحظات: {Notes}\n")}{separator}

📊 ملخص الحساب (DEBT SUMMARY):
------------------------------------------
إجمالي الديون السابقة:    {TotalDebt:N2} د.م
عدد الفواتير المتبقية:    {UnpaidInvoicesCount}
{separator}

💰 تفاصيل الدفعة الحالية:
------------------------------------------
المبلغ المدفوع الآن:     {AmountToPay:N2} د.م
طريقة الدفع:            {SelectedPaymentMethod}
{separator}

✅ الحالة بعد الدفع:
------------------------------------------
المتبقي بذمة العميل:     {RemainingAfter:N2} د.م
نسبة سداد هذه الدفعة:    {(TotalDebt > 0 ? (AmountToPay / TotalDebt * 100) : 0):N1}%
{doubleSeparator}

المستلم: {POS_BIGA.Services.SessionService.CurrentUserDisplayName}
طريقة التوزيع: الأقدم أولاً (FIFO)

{separator}
        شكراً لثقتكم بنا
       نقدر تعاملكم الدائم
{separator}
";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

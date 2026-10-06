using System;
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
using POS_BIGA.Views.Windows;
using POS_BIGA.Infrastructure.Printing.Abstractions;
using POS_BIGA.Infrastructure.Printing.Services;

namespace POS_BIGA.ViewModels
{
    public class CustomerDetailsViewModel : INotifyPropertyChanged
    {
        private readonly int _customerId;

        #region Properties

        private Customer _customer;
        public Customer Customer
        {
            get => _customer;
            set { _customer = value; OnPropertyChanged(nameof(Customer)); }
        }

        public ObservableCollection<SaleDto> Sales { get; } = new();
        public ObservableCollection<CreditDto> Credits { get; } = new();

        // ✅ إحصائيات الديون المحسوبة من Credits
        public decimal TotalDebt      => Credits.Sum(c => c.OriginalAmount);
        public decimal TotalPaid      => Credits.Sum(c => c.PaidAmount);
        public decimal TotalRemaining => Credits.Sum(c => c.RemainingAmount);
        public bool    HasUnpaidCredits => Credits.Any(c => c.RemainingAmount > 0);

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(nameof(SelectedTabIndex)); }
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(nameof(DialogResult)); }
        }

        #endregion

        #region Commands

        public ICommand RefreshCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand PayCreditCommand { get; }
        public ICommand ViewInvoiceCommand { get; }
        public ICommand ExportCreditsCommand { get; }
        public ICommand PrintSaleCommand { get; }
        public ICommand ViewSaleDetailsCommand { get; }

        #endregion

        public CustomerDetailsViewModel(int customerId)
        {
            _customerId = customerId;

            RefreshCommand = new RelayCommand(_ => LoadData());
            EditCustomerCommand = new RelayCommand(_ => EditCustomer());
            CloseCommand = new RelayCommand(_ => DialogResult = false);
            AddPaymentCommand = new RelayCommand(_ => AddPayment());
            PayCreditCommand = new RelayCommand(param => PayCredit(param as CreditDto));
            ViewInvoiceCommand = new RelayCommand(param => ViewInvoice(param as CreditDto));
            ExportCreditsCommand = new RelayCommand(_ => ExportCredits());
            PrintSaleCommand = new RelayCommand(param => _ = PrintSaleAsync(param as SaleDto));
            ViewSaleDetailsCommand = new RelayCommand(param => _ = ViewSaleDetailsAsync(param as SaleDto));

            LoadData();
        }

        public void LoadData()
        {
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var ctx = new AppDbContext();

                Customer = await ctx.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == _customerId);

                if (Customer == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على العميل!", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    return;
                }

                // Load Sales
                var sales = await ctx.Sales
                    .Where(s => s.CustomerId == _customerId)
                    .OrderByDescending(s => s.SaleDate)
                    .Select(s => new SaleDto
                    {
                        Id = s.Id,
                        SaleNumber = $"INV-{s.Id:D6}",
                        SaleDate = s.SaleDate,
                        TotalAmount = s.TotalAmount,
                        PaymentMethod = s.Payments.FirstOrDefault().PaymentMethod.ToString()
                    })
                    .ToListAsync();

                Sales.Clear();
                foreach (var sale in sales)
                    Sales.Add(sale);

                // Load Credits
                var credits = await ctx.Credits
                    .Where(c => c.CustomerId == _customerId)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CreditDto
                    {
                        Id = c.Id,
                        SaleNumber = $"INV-{c.SaleId:D6}",
                        Date = c.CreatedAt,
                        OriginalAmount = c.OriginalAmount,
                        PaidAmount = c.PaidAmount,
                        RemainingAmount = c.RemainingAmount,
                        Status = c.Status.ToString()
                    })
                    .ToListAsync();

                Credits.Clear();
                foreach (var credit in credits)
                    Credits.Add(credit);

                // ✅ إشعار الـ UI بتحديث الإحصائيات المحسوبة
                OnPropertyChanged(nameof(TotalDebt));
                OnPropertyChanged(nameof(TotalPaid));
                OnPropertyChanged(nameof(TotalRemaining));
                OnPropertyChanged(nameof(HasUnpaidCredits));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في تحميل البيانات:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCustomer()
        {
            if (Customer == null) return;

            try
            {
                var editor = new CustomerEditorWindow(Customer.Id);
                if (editor.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في فتح نافذة التعديل:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPayment()
        {
            try
            {
                var paymentWindow = new CreditPaymentWindow(Customer.Id, Customer.FullName);
                if (paymentWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في فتح نافذة الدفع:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PayCredit(CreditDto credit)
        {
            if (credit == null || !credit.HasRemainingAmount) return;

            try
            {
                var paymentWindow = new CreditPaymentWindow(Customer.Id, Customer.FullName);
                if (paymentWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في فتح نافذة الدفع:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewInvoice(CreditDto credit)
        {
            if (credit == null) return;

            try
            {
                MessageBox.Show(
                    $"📄 معلومات الفاتورة:\n\n" +
                    $"رقم الفاتورة: {credit.SaleNumber}\n" +
                    $"التاريخ: {credit.Date:yyyy-MM-dd}\n" +
                    $"المبلغ الأصلي: {credit.OriginalAmount:N2} د.م\n" +
                    $"المدفوع: {credit.PaidAmount:N2} د.م\n" +
                    $"المتبقي: {credit.RemainingAmount:N2} د.م\n" +
                    $"الحالة: {credit.Status}",
                    "تفاصيل الفاتورة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في عرض الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCredits()
        {
            MessageBox.Show("سيتم إضافة التصدير قريباً...", "قيد التطوير",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task PrintSaleAsync(SaleDto saleDto)
        {
            if (saleDto == null) return;

            try
            {
                using var ctx = new AppDbContext();

                var sale = await ctx.Sales
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Product)
                    .Include(s => s.Payments)
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.Id == saleDto.Id);

                if (sale == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على الفاتورة!", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Use PrinterService Singleton
                var printerService = PrinterService.Instance;

                await printerService.PrintReceiptAsync(sale);

                MessageBox.Show($"✅ تم إعادة طباعة الفاتورة {saleDto.SaleNumber} بنجاح!", 
                    "نجحت الطباعة",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في طباعة الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ViewSaleDetailsAsync(SaleDto saleDto)
        {
            if (saleDto == null) return;

            try
            {
                using var ctx = new AppDbContext();

                var sale = await ctx.Sales
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Product)
                    .Include(s => s.Payments)
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.Id == saleDto.Id);

                if (sale == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على الفاتورة!", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build detailed invoice info
                var details = $"📄 تفاصيل الفاتورة\n";
                details += $"════════════════════════════\n\n";
                details += $"🔢 رقم الفاتورة: {saleDto.SaleNumber}\n";
                details += $"📅 التاريخ: {sale.SaleDate:yyyy-MM-dd HH:mm}\n";
                details += $"👤 العميل: {sale.Customer?.FullName ?? "زبون عادي"}\n";
                details += $"💳 طريقة الدفع: {sale.Payments?.FirstOrDefault()?.PaymentMethod ?? PaymentMethod.Cash}\n\n";

                details += $"📦 المنتجات:\n";
                details += $"────────────────────────────\n";

                if (sale.Items != null && sale.Items.Any())
                {
                    foreach (var item in sale.Items)
                    {
                        details += $"• {item.ProductName ?? "منتج"}\n";
                        details += $"  الكمية: {item.Quantity:N2} × {item.UnitPrice:N2} = {item.LineTotal:N2} د.م\n";
                    }
                }

                details += $"\n────────────────────────────\n";
                details += $"💰 الإجمالي الفرعي: {sale.SubTotal:N2} د.م\n";
                details += $"🎁 الخصم: {sale.DiscountAmount:N2} د.م\n";
                details += $"💵 الضريبة: {sale.TaxAmount:N2} د.م\n";
                details += $"════════════════════════════\n";
                details += $"✅ المبلغ الإجمالي: {sale.TotalAmount:N2} د.م\n";
                details += $"💵 المدفوع: {sale.PaidAmount:N2} د.م\n";
                details += $"⏳ المتبقي: {sale.RemainingAmount:N2} د.م\n";

                MessageBox.Show(details, "تفاصيل الفاتورة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في عرض تفاصيل الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SaleDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class CreditDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; }
        public DateTime Date { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; }
        public bool HasRemainingAmount => RemainingAmount > 0;
    }
}

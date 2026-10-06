using POS_BIGA.Commands;
using POS_BIGA.Data;
using POS_BIGA.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Windows;
using POS_BIGA.Infrastructure.Printing.Services;
using System.Windows.Documents;
using System.Windows.Media;
using POS_BIGA.Infrastructure.Printing.Pdf;
using POS_BIGA.Infrastructure.Excel;
using System.IO;

namespace POS_BIGA.ViewModels
{
    /// <summary>
    /// نموذج بيانات متقدم للتقارير مع ميزات احترافية
    /// </summary>
    public class AdvancedReportsViewModel : INotifyPropertyChanged
    {
        #region Fields
        
        private decimal _totalSales;
        private decimal _totalProfit;
        private decimal _totalCost;
        private int _invoicesCount;
        private decimal _itemsSold;
        private decimal _averageSale;
        private decimal _salesGrowth;
        private decimal _profitMarginPercent;

        // Credit statistics
        private decimal _totalCredits;
        private decimal _paidCredits;
        private decimal _remainingCredits;
        private int _creditCustomersCount;
        private decimal _creditPaymentRate;
        
        private DateTime? _startDate = DateTime.Today.AddDays(-30);
        private DateTime? _endDate = DateTime.Today;
        private string _selectedPeriod = "آخر 30 يوم";
        
        private bool _isLoading;
        private string _statusMessage;
        
        #endregion
        
        #region Properties
        
        public decimal TotalSales
        {
            get => _totalSales;
            set 
            { 
                _totalSales = value; 
                OnPropertyChanged(nameof(TotalSales));
                UpdateCalculatedFields();
            }
        }
        
        public decimal TotalProfit
        {
            get => _totalProfit;
            set 
            { 
                _totalProfit = value; 
                OnPropertyChanged(nameof(TotalProfit));
                UpdateCalculatedFields();
            }
        }
        
        public decimal TotalCost
        {
            get => _totalCost;
            set 
            { 
                _totalCost = value; 
                OnPropertyChanged(nameof(TotalCost));
            }
        }
        
        public int InvoicesCount
        {
            get => _invoicesCount;
            set 
            { 
                _invoicesCount = value; 
                OnPropertyChanged(nameof(InvoicesCount));
                UpdateCalculatedFields();
            }
        }
        
        public decimal ItemsSold
        {
            get => _itemsSold;
            set 
            { 
                _itemsSold = value; 
                OnPropertyChanged(nameof(ItemsSold));
            }
        }
        
        public decimal AverageSale
        {
            get => _averageSale;
            set 
            { 
                _averageSale = value; 
                OnPropertyChanged(nameof(AverageSale));
            }
        }
        
        public decimal SalesGrowth
        {
            get => _salesGrowth;
            set 
            { 
                _salesGrowth = value; 
                OnPropertyChanged(nameof(SalesGrowth));
                OnPropertyChanged(nameof(SalesGrowthFormatted));
            }
        }
        
        public string SalesGrowthFormatted => SalesGrowth >= 0 ? $"+{SalesGrowth:N1}%" : $"{SalesGrowth:N1}%";
        
        public decimal ProfitMarginPercent
        {
            get => _profitMarginPercent;
            set 
            { 
                _profitMarginPercent = value; 
                OnPropertyChanged(nameof(ProfitMarginPercent));
                OnPropertyChanged(nameof(ProfitMarginFormatted));
            }
        }
        
        public string ProfitMarginFormatted => $"{ProfitMarginPercent:N1}%";

        public decimal TotalCredits
        {
            get => _totalCredits;
            set 
            { 
                _totalCredits = value; 
                OnPropertyChanged(nameof(TotalCredits));
                UpdateCreditRate();
            }
        }

        public decimal PaidCredits
        {
            get => _paidCredits;
            set 
            { 
                _paidCredits = value; 
                OnPropertyChanged(nameof(PaidCredits));
                UpdateCreditRate();
            }
        }

        public decimal RemainingCredits
        {
            get => _remainingCredits;
            set 
            { 
                _remainingCredits = value; 
                OnPropertyChanged(nameof(RemainingCredits));
            }
        }

        public int CreditCustomersCount
        {
            get => _creditCustomersCount;
            set 
            { 
                _creditCustomersCount = value; 
                OnPropertyChanged(nameof(CreditCustomersCount));
            }
        }

        public decimal CreditPaymentRate
        {
            get => _creditPaymentRate;
            set 
            { 
                _creditPaymentRate = value; 
                OnPropertyChanged(nameof(CreditPaymentRate));
                OnPropertyChanged(nameof(CreditPaymentRateFormatted));
            }
        }

        public string CreditPaymentRateFormatted => $"{CreditPaymentRate:N1}%";

        private void UpdateCreditRate()
        {
            if (TotalCredits > 0)
            {
                CreditPaymentRate = (PaidCredits / TotalCredits) * 100;
            }
        }
        
        public DateTime? StartDate
        {
            get => _startDate;
            set 
            { 
                _startDate = value; 
                OnPropertyChanged(nameof(StartDate));
            }
        }
        
        public DateTime? EndDate
        {
            get => _endDate;
            set 
            { 
                _endDate = value; 
                OnPropertyChanged(nameof(EndDate));
            }
        }
        
        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set 
            { 
                _selectedPeriod = value; 
                OnPropertyChanged(nameof(SelectedPeriod));
                ApplyPeriodFilter(value);
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set 
            { 
                _isLoading = value; 
                OnPropertyChanged(nameof(IsLoading));
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set 
            { 
                _statusMessage = value; 
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        
        // Collections
        public ObservableCollection<SaleReportDto> Sales { get; } = new ObservableCollection<SaleReportDto>();
        public ObservableCollection<AdvancedTopProductDto> TopProducts { get; } = new ObservableCollection<AdvancedTopProductDto>();
        public ObservableCollection<CategorySalesDto> CategorySales { get; } = new ObservableCollection<CategorySalesDto>();
        public ObservableCollection<DailySalesDto> DailySales { get; } = new ObservableCollection<DailySalesDto>();
        public ObservableCollection<HourlySalesDto> HourlySales { get; } = new ObservableCollection<HourlySalesDto>();
        public ObservableCollection<CreditReportDto> Credits { get; } = new ObservableCollection<CreditReportDto>();
        
        public ObservableCollection<string> PeriodOptions { get; } = new ObservableCollection<string>
        {
            "اليوم",
            "أمس",
            "آخر 7 أيام",
            "آخر 30 يوم",
            "هذا الشهر",
            "الشهر الماضي",
            "هذه السنة",
            "مخصص"
        };
        
        #endregion
        
        #region Commands

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand PrintThermalCommand { get; }
        public ICommand ComparePeriodsCommand { get; }
        public ICommand ReprintInvoiceCommand { get; }
        public ICommand ViewInvoiceDetailsCommand { get; }
        public ICommand PreviewInvoiceCommand { get; }
        public ICommand DeleteSaleCommand { get; }
        public ICommand ExportInvoicesPeriodPdfCommand { get; }
        public ICommand ExportCreditsExcelCommand { get; }
        public ICommand ExportCreditsPdfCommand { get; }
        public ICommand PrintCreditsCommand { get; }

        #endregion

        #region Constructor

        public AdvancedReportsViewModel()
        {
            LoadCommand = new RelayCommand(async _ => await LoadReportsAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadReportsAsync());
            ExportExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync());
            ExportPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            PrintCommand = new RelayCommand(_ => PrintReport());
            PrintThermalCommand = new RelayCommand(_ => PrintThermalReport());
            ComparePeriodsCommand = new RelayCommand(_ => ComparePeriods());
            ReprintInvoiceCommand = new RelayCommand(async param => await ReprintInvoiceAsync(param as SaleReportDto));
            ViewInvoiceDetailsCommand = new RelayCommand(async param => await ViewInvoiceDetailsAsync(param as SaleReportDto));
            PreviewInvoiceCommand = new RelayCommand(async param => await PreviewInvoiceAsync(param as SaleReportDto));
            DeleteSaleCommand = new RelayCommand(async param => await DeleteSaleAsync(param as SaleReportDto));
            ExportInvoicesPeriodPdfCommand = new RelayCommand(async _ => await ExportInvoicesPeriodPdfAsync());
            ExportCreditsExcelCommand = new RelayCommand(async _ => await ExportCreditsExcelAsync());
            ExportCreditsPdfCommand = new RelayCommand(async _ => await ExportCreditsPdfAsync());
            PrintCreditsCommand = new RelayCommand(_ => PrintCredits());

            // Load initial data
            _ = LoadReportsAsync();
        }

        #endregion
        
        #region Methods
        
        private void ApplyPeriodFilter(string period)
        {
            var now = DateTime.Now;
            
            switch (period)
            {
                case "اليوم":
                    StartDate = now.Date;
                    EndDate = now.Date;
                    break;
                    
                case "أمس":
                    StartDate = now.Date.AddDays(-1);
                    EndDate = now.Date.AddDays(-1);
                    break;
                    
                case "آخر 7 أيام":
                    StartDate = now.Date.AddDays(-7);
                    EndDate = now.Date;
                    break;
                    
                case "آخر 30 يوم":
                    StartDate = now.Date.AddDays(-30);
                    EndDate = now.Date;
                    break;
                    
                case "هذا الشهر":
                    StartDate = new DateTime(now.Year, now.Month, 1);
                    EndDate = now.Date;
                    break;
                    
                case "الشهر الماضي":
                    var lastMonth = now.AddMonths(-1);
                    StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    EndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                    
                case "هذه السنة":
                    StartDate = new DateTime(now.Year, 1, 1);
                    EndDate = now.Date;
                    break;
            }
            
            if (period != "مخصص")
            {
                _ = LoadReportsAsync();
            }
        }
        
        public async Task LoadReportsAsync()
        {
            IsLoading = true;
            StatusMessage = "جارٍ تحميل التقارير...";
            
            try
            {
                // Reset all data
                ResetData();
                
                using var ctx = new AppDbContext();
                var from = StartDate?.Date ?? DateTime.Today.AddDays(-30);
                var to = (EndDate?.Date ?? DateTime.Today).AddDays(1);
                
                System.Diagnostics.Debug.WriteLine($"📅 Loading reports from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
                
                // Load sales with all relationships
                var sales = await ctx.Sales
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Category)
                    .Include(s => s.Customer)
                    .Include(s => s.Payments)
                    .Where(s => s.SaleDate >= from && s.SaleDate < to)
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();
                
                if (!sales.Any())
                {
                    StatusMessage = "لا توجد مبيعات في الفترة المحددة";
                    IsLoading = false;
                    return;
                }
                
                // Calculate main statistics
                await CalculateStatistics(sales);
                
                // Load detailed reports
                await LoadSalesDetails(sales);
                await LoadTopProducts(sales);
                await LoadCategorySales(sales);
                await LoadDailySales(sales);
                await LoadHourlySales(sales);
                await LoadCreditsReport(ctx, from, to);

                // Calculate growth compared to previous period
                await CalculateGrowth(ctx, from, to);
                
                StatusMessage = $"تم تحميل {sales.Count} فاتورة بنجاح";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"❌ Error loading reports: {ex}");
                MessageBox.Show(
                    $"حدث خطأ أثناء تحميل التقارير:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void ResetData()
        {
            TotalSales = 0;
            TotalProfit = 0;
            TotalCost = 0;
            InvoicesCount = 0;
            ItemsSold = 0;
            AverageSale = 0;
            SalesGrowth = 0;
            ProfitMarginPercent = 0;

            TotalCredits = 0;
            PaidCredits = 0;
            RemainingCredits = 0;
            CreditCustomersCount = 0;
            CreditPaymentRate = 0;

            Sales.Clear();
            TopProducts.Clear();
            CategorySales.Clear();
            DailySales.Clear();
            HourlySales.Clear();
            Credits.Clear();
        }
        
        private Task CalculateStatistics(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                decimal totalRevenue = 0;
                decimal totalCostValue = 0;
                decimal totalProfitValue = 0;
                int itemsCount = 0;

                foreach (var sale in sales)
                {
                    totalRevenue += sale.TotalAmount;

                    foreach (var item in sale.Items)
                    {
                        var cost = item.Product?.PurchasePrice ?? 0;
                        var quantity = item.Quantity;

                        totalCostValue += cost * quantity;
                        totalProfitValue += (item.UnitPrice - cost) * quantity;
                        itemsCount += (int)quantity;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📊 Statistics Calculated:");
                System.Diagnostics.Debug.WriteLine($"   Total Revenue: {totalRevenue:N2}");
                System.Diagnostics.Debug.WriteLine($"   Total Cost: {totalCostValue:N2}");
                System.Diagnostics.Debug.WriteLine($"   Total Profit: {totalProfitValue:N2}");
                System.Diagnostics.Debug.WriteLine($"   Invoices: {sales.Count}");
                System.Diagnostics.Debug.WriteLine($"   Items Sold: {itemsCount}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalSales = totalRevenue;
                    TotalCost = totalCostValue;
                    TotalProfit = totalProfitValue;
                    InvoicesCount = sales.Count;
                    ItemsSold = itemsCount;
                    AverageSale = sales.Count > 0 ? totalRevenue / sales.Count : 0;
                    ProfitMarginPercent = totalRevenue > 0 ? (totalProfitValue / totalRevenue) * 100 : 0;
                });
            });
        }

        private Task LoadSalesDetails(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                var salesDtos = new List<SaleReportDto>();
                
                foreach (var sale in sales)
                {
                    salesDtos.Add(new SaleReportDto
                    {
                        SaleId = sale.Id,
                        SaleNumber = sale.SaleNumber,
                        SaleDate = sale.SaleDate,
                        CustomerName = sale.Customer?.FullName ?? "زبون عابر",
                        TotalAmount = sale.TotalAmount,
                        PaidAmount = sale.PaidAmount,
                        RemainingAmount = sale.RemainingAmount,
                        ItemsCount = sale.Items.Count,
                        PaymentMethod = ConvertPaymentMethod(sale.Payments?.FirstOrDefault()?.Method ?? PaymentMethod.Cash),
                        TotalProfit = sale.Items.Sum(i => (i.UnitPrice - (i.Product?.PurchasePrice ?? 0)) * i.Quantity),
                        ProfitMarginFormatted = sale.TotalAmount > 0
                            ? $"{(sale.Items.Sum(i => (i.UnitPrice - (i.Product?.PurchasePrice ?? 0)) * i.Quantity) / sale.TotalAmount * 100):N1}%"
                            : "0.0%"
                    });
                }
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var dto in salesDtos)
                    {
                        Sales.Add(dto);
                    }
                });
            });
        }
        
        private Task LoadTopProducts(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                var productStats = new Dictionary<int, (string Name, decimal Quantity, decimal Revenue, decimal Profit)>();
                
                foreach (var sale in sales)
                {
                    foreach (var item in sale.Items)
                    {
                        if (item.Product == null) continue;
                        
                        if (!productStats.ContainsKey(item.ProductId))
                        {
                            productStats[item.ProductId] = (item.Product.Name, 0, 0, 0);
                        }
                        
                        var current = productStats[item.ProductId];
                        var itemProfit = item.LineTotalAfterDiscount - (item.Product.PurchasePrice * item.Quantity);
                        
                        productStats[item.ProductId] = (
                            current.Name,
                            current.Quantity + item.Quantity,
                            current.Revenue + item.LineTotalAfterDiscount,
                            current.Profit + itemProfit
                        );
                    }
                }
                
                var topProducts = productStats
                    .OrderByDescending(p => p.Value.Revenue)
                    .Take(10)
                    .Select(p => new AdvancedTopProductDto
                    {
                        ProductName = p.Value.Name,
                        QuantitySold = p.Value.Quantity,
                        TotalRevenue = p.Value.Revenue,
                        TotalProfit = p.Value.Profit,
                        ProfitMargin = p.Value.Revenue > 0 ? (p.Value.Profit / p.Value.Revenue) * 100 : 0
                    })
                    .ToList();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var product in topProducts)
                    {
                        TopProducts.Add(product);
                    }
                });
            });
        }
        
        private Task LoadCategorySales(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                var categoryStats = new Dictionary<string, (decimal Revenue, int ItemsCount)>();
                
                foreach (var sale in sales)
                {
                    foreach (var item in sale.Items)
                    {
                        var categoryName = item.Product?.Category?.Name ?? "غير مصنف";
                        
                        if (!categoryStats.ContainsKey(categoryName))
                        {
                            categoryStats[categoryName] = (0, 0);
                        }
                        
                        var current = categoryStats[categoryName];
                        categoryStats[categoryName] = (
                            current.Revenue + item.LineTotalAfterDiscount,
                            current.ItemsCount + 1
                        );
                    }
                }
                
                var total = categoryStats.Sum(c => c.Value.Revenue);
                
                var categorySales = categoryStats
                    .OrderByDescending(c => c.Value.Revenue)
                    .Select(c => new CategorySalesDto
                    {
                        CategoryName = c.Key,
                        TotalRevenue = c.Value.Revenue,
                        ItemsCount = c.Value.ItemsCount,
                        Percentage = total > 0 ? (c.Value.Revenue / total) * 100 : 0
                    })
                    .ToList();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var category in categorySales)
                    {
                        CategorySales.Add(category);
                    }
                });
            });
        }
        
        private Task LoadDailySales(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                var dailyStats = sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new DailySalesDto
                    {
                        Date = g.Key,
                        TotalSales = g.Sum(s => s.TotalAmount),
                        InvoicesCount = g.Count(),
                        AverageSale = g.Average(s => s.TotalAmount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var day in dailyStats)
                    {
                        DailySales.Add(day);
                    }
                });
            });
        }
        
        private Task LoadHourlySales(List<Sale> sales)
        {
            return Task.Run(() =>
            {
                var hourlyStats = sales
                    .GroupBy(s => s.SaleDate.Hour)
                    .Select(g => new HourlySalesDto
                    {
                        Hour = g.Key,
                        TotalSales = g.Sum(s => s.TotalAmount),
                        InvoicesCount = g.Count()
                    })
                    .OrderBy(h => h.Hour)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var hour in hourlyStats)
                    {
                        HourlySales.Add(hour);
                    }
                });
            });
        }

        private async Task LoadCreditsReport(AppDbContext ctx, DateTime from, DateTime to)
        {
            try
            {
                // Load all credits in the period
                var credits = await ctx.Credits
                    .Include(c => c.Customer)
                    .Include(c => c.Sale)
                    .Include(c => c.Payments)
                    .Where(c => c.CreatedAt >= from && c.CreatedAt < to)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                if (!credits.Any())
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ No credits found in the selected period");
                    return;
                }

                decimal totalCredits = 0;
                decimal paidCredits = 0;
                decimal remainingCredits = 0;
                var customerIds = new HashSet<int>();

                var creditDtos = new List<CreditReportDto>();

                foreach (var credit in credits)
                {
                    totalCredits += credit.OriginalAmount;
                    paidCredits += credit.PaidAmount;
                    remainingCredits += credit.RemainingAmount;

                    if (credit.CustomerId > 0)
                    {
                        customerIds.Add(credit.CustomerId);
                    }

                    creditDtos.Add(new CreditReportDto
                    {
                        CreditId = credit.Id,
                        CustomerName = credit.Customer?.FullName ?? "غير محدد",
                        SaleNumber = credit.Sale?.SaleNumber ?? "N/A",
                        OriginalAmount = credit.OriginalAmount,
                        PaidAmount = credit.PaidAmount,
                        RemainingAmount = credit.RemainingAmount,
                        CreatedDate = credit.CreatedAt,
                        DueDate = credit.DueDate,
                        Status = GetCreditStatusText(credit.Status),
                        StatusColor = GetCreditStatusColor(credit.Status)
                    });
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalCredits = totalCredits;
                    PaidCredits = paidCredits;
                    RemainingCredits = remainingCredits;
                    CreditCustomersCount = customerIds.Count;

                    foreach (var dto in creditDtos)
                    {
                        Credits.Add(dto);
                    }
                });

                System.Diagnostics.Debug.WriteLine($"💳 Total Credits: {totalCredits:N2} د.م");
                System.Diagnostics.Debug.WriteLine($"✅ Paid Credits: {paidCredits:N2} د.م");
                System.Diagnostics.Debug.WriteLine($"⏳ Remaining Credits: {remainingCredits:N2} د.م");
                System.Diagnostics.Debug.WriteLine($"👥 Credit Customers: {customerIds.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading credits: {ex.Message}");
            }
        }

        private string GetCreditStatusText(CreditStatus status)
        {
            return status switch
            {
                CreditStatus.Unpaid => "غير مدفوع",
                CreditStatus.PartiallyPaid => "مدفوع جزئياً",
                CreditStatus.Paid => "مدفوع",
                CreditStatus.Overdue => "متأخر",
                _ => "غير معروف"
            };
        }

        private string GetCreditStatusColor(CreditStatus status)
        {
            return status switch
            {
                CreditStatus.Unpaid => "#E74C3C",      // Red
                CreditStatus.PartiallyPaid => "#F39C12", // Orange
                CreditStatus.Paid => "#27AE60",        // Green
                CreditStatus.Overdue => "#C0392B",     // Dark Red
                _ => "#95A5A6"                         // Gray
            };
        }
        
        private async Task CalculateGrowth(AppDbContext ctx, DateTime from, DateTime to)
        {
            try
            {
                var periodDays = (to - from).Days;
                var previousFrom = from.AddDays(-periodDays);
                var previousTo = from;
                
                var previousSales = await ctx.Sales
                    .Where(s => s.SaleDate >= previousFrom && s.SaleDate < previousTo)
                    .SumAsync(s => s.TotalAmount);
                
                if (previousSales > 0)
                {
                    var growth = ((TotalSales - previousSales) / previousSales) * 100;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SalesGrowth = growth;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating growth: {ex.Message}");
            }
        }
        
        private void UpdateCalculatedFields()
        {
            if (InvoicesCount > 0)
            {
                AverageSale = TotalSales / InvoicesCount;
            }
            
            if (TotalSales > 0)
            {
                ProfitMarginPercent = (TotalProfit / TotalSales) * 100;
            }
        }
        
        private string ConvertPaymentMethod(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "نقدي",
                PaymentMethod.Card => "بطاقة",
                PaymentMethod.Mobile => "محفظة",
                PaymentMethod.Voucher => "قسيمة",
                _ => "غير محدد"
            };
        }
        
        /// <summary>
        /// فتح الملف الناتج بعد التصدير (يفتح Excel/PDF الافتراضي على جهاز المستخدم).
        /// </summary>
        private static void OpenExportedFile(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { /* لا مشكلة إن تعذّر فتح الملف تلقائياً، يبقى محفوظاً في المسار المعروض */ }
        }

        private static string BuildExportPath(string prefix, string extension)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return System.IO.Path.Combine(folder, $"{prefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{extension}");
        }

        private async Task ExportToExcelAsync()
        {
            if (Sales.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "جارٍ تصدير التقرير إلى Excel...";

            try
            {
                var data = BuildReportData();
                var salesSnapshot = Sales.ToList();
                var creditsSnapshot = Credits.ToList();
                var path = BuildExportPath("تقرير_المبيعات_الشامل", "xlsx");

                await Task.Run(() => ReportExcelExporter.SaveFullReport(data, salesSnapshot, creditsSnapshot, path));

                MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{path}", "تصدير ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenExportedFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        private async Task ExportToPdfAsync()
        {
            if (Sales.Count == 0)
            {
                MessageBox.Show("لا توجد بيانات لتصديرها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "جارٍ تصدير التقرير إلى PDF...";

            try
            {
                var data = BuildReportData();
                var settings = PrinterService.Instance.LoadSettings();
                var path = BuildExportPath("تقرير_المبيعات_الشامل", "pdf");

                await Task.Run(() => ReportQuestPdfBuilder.SaveSummaryPdf(data, settings, path));

                MessageBox.Show($"تم تصدير التقرير بنجاح إلى:\n{path}", "تصدير ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenExportedFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>تصدير قائمة فواتير الفترة الحالية إلى PDF (تبويب "تفاصيل الفواتير").</summary>
        private async Task ExportInvoicesPeriodPdfAsync()
        {
            if (Sales.Count == 0)
            {
                MessageBox.Show("لا توجد فواتير لتصديرها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "جارٍ تصدير الفواتير إلى PDF...";

            try
            {
                var salesSnapshot = Sales.ToList();
                var settings = PrinterService.Instance.LoadSettings();
                var start = StartDate ?? DateTime.Today.AddDays(-30);
                var end = EndDate ?? DateTime.Today;
                var path = BuildExportPath("تقرير_الفواتير", "pdf");

                await Task.Run(() => ReportQuestPdfBuilder.SaveInvoicesListPdf(salesSnapshot, start, end, settings, path));

                MessageBox.Show($"تم تصدير الفواتير بنجاح إلى:\n{path}", "تصدير ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenExportedFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>تصدير قائمة الديون الحالية إلى Excel (تبويب "تفاصيل الديون").</summary>
        private async Task ExportCreditsExcelAsync()
        {
            if (Credits.Count == 0)
            {
                MessageBox.Show("لا توجد ديون لتصديرها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "جارٍ تصدير الديون إلى Excel...";

            try
            {
                var creditsSnapshot = Credits.ToList();
                var path = BuildExportPath("تقرير_الديون", "xlsx");

                await Task.Run(() => ReportExcelExporter.SaveCreditsOnly(creditsSnapshot, path));

                MessageBox.Show($"تم تصدير الديون بنجاح إلى:\n{path}", "تصدير ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenExportedFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>تصدير قائمة الديون الحالية إلى PDF (تبويب "تفاصيل الديون").</summary>
        private async Task ExportCreditsPdfAsync()
        {
            if (Credits.Count == 0)
            {
                MessageBox.Show("لا توجد ديون لتصديرها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;
            StatusMessage = "جارٍ تصدير الديون إلى PDF...";

            try
            {
                var creditsSnapshot = Credits.ToList();
                var settings = PrinterService.Instance.LoadSettings();
                var path = BuildExportPath("تقرير_الديون", "pdf");

                await Task.Run(() => ReportQuestPdfBuilder.SaveCreditsListPdf(creditsSnapshot, settings, path));

                MessageBox.Show($"تم تصدير الديون بنجاح إلى:\n{path}", "تصدير ناجح", MessageBoxButton.OK, MessageBoxImage.Information);
                OpenExportedFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>طباعة/معاينة ملخص ديون العملاء لتبويب "تفاصيل الديون" (يعيد استخدام نفس محرك طباعة تقرير الملخص).</summary>
        private void PrintCredits()
        {
            if (Credits.Count == 0)
            {
                MessageBox.Show("لا توجد ديون لطباعتها في الفترة المحددة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var printerService = PrinterService.Instance;
                var baseSettings = printerService.LoadSettings();

                // نسخة من الإعدادات تُظهر قسم الديون فقط (بدون الملخص/المنتجات/الفئات)
                var settings = CloneSettingsForCreditsOnly(baseSettings);

                var reportData = BuildReportData();
                var template = new Infrastructure.Printing.Templates.CompactTemplate();
                var doc = template.BuildReportPreview(reportData, settings);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var previewWin = new Infrastructure.Printing.Views.DocumentPrintPreviewWindow(
                        doc, "تقرير الديون", settings.ReceiptPrinter);
                    previewWin.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في طباعة تقرير الديون:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Infrastructure.Printing.Settings.PrintSettings CloneSettingsForCreditsOnly(Infrastructure.Printing.Settings.PrintSettings baseSettings)
        {
            var s = baseSettings ?? new Infrastructure.Printing.Settings.PrintSettings();
            return new Infrastructure.Printing.Settings.PrintSettings
            {
                ShopName = s.ShopName,
                ShopAddress = s.ShopAddress,
                ShopPhone = s.ShopPhone,
                Currency = s.Currency,
                CurrencySymbol = s.CurrencySymbol,
                DecimalPlaces = s.DecimalPlaces,
                PaperSize = s.PaperSize,
                ReceiptPrinter = s.ReceiptPrinter,
                A4ReportShowHeader = true,
                A4ReportShowSummary = false,
                A4ReportShowTopProducts = false,
                A4ReportShowCategories = false,
                A4ReportShowDailySales = false,
                A4ReportShowCredits = true,
                A4ReportShowCreditsList = true,
            };
        }
        
        private void PrintReport()
        {
            try
            {
                var printerService = PrinterService.Instance;
                var settings       = printerService.LoadSettings();

                // استخدام CompactTemplate لبناء تقرير A4 احترافي مع سويتش الأقسام
                var reportData = BuildReportData();
                var template   = new Infrastructure.Printing.Templates.CompactTemplate();
                var doc        = template.BuildReportPreview(reportData, settings);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var previewWin = new Infrastructure.Printing.Views.DocumentPrintPreviewWindow(
                        doc, "تقرير المبيعات - A4", settings.ReceiptPrinter);
                    previewWin.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في طباعة التقرير:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintThermalReport()
        {
            try
            {
                var printerService = PrinterService.Instance;
                var settings       = printerService.LoadSettings();

                // زر حراري — يستخدم دائماً ReportSimpleTemplate
                var reportData = BuildReportData();
                var template   = new Infrastructure.Printing.Templates.ReportSimpleTemplate();
                var doc        = template.Build(reportData, settings);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var previewWin = new Infrastructure.Printing.Views.DocumentPrintPreviewWindow(
                        doc, "تقرير المبيعات - حراري", settings.ReceiptPrinter);
                    previewWin.ShowDialog();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في الطباعة الحرارية:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// يبني <see cref="ReportData"/> من البيانات المحملة حالياً في الـ ViewModel
        /// </summary>
        private Infrastructure.Printing.Templates.ReportData BuildReportData()
        {
            return new Infrastructure.Printing.Templates.ReportData
            {
                StartDate          = StartDate ?? DateTime.Today.AddDays(-30),
                EndDate            = EndDate   ?? DateTime.Today,
                TotalSales         = TotalSales,
                TotalProfit        = TotalProfit,
                InvoicesCount      = InvoicesCount,
                AverageSale        = AverageSale,
                ProfitMarginPercent= ProfitMarginPercent,
                SalesGrowth        = SalesGrowth,
                TotalCredits       = TotalCredits,
                PaidCredits        = PaidCredits,
                RemainingCredits   = RemainingCredits,
                CreditPaymentRate  = CreditPaymentRate,
                TopProducts = TopProducts.Select(p => new Infrastructure.Printing.Templates.ReportTopProduct
                {
                    Name         = p.ProductName,
                    QuantitySold = p.QuantitySold,
                    TotalRevenue = p.TotalRevenue
                }).ToList(),
                CategorySales = CategorySales.Select(c => new Infrastructure.Printing.Templates.ReportCategorySales
                {
                    CategoryName = c.CategoryName,
                    TotalRevenue = c.TotalRevenue,
                    Percentage   = c.Percentage
                }).ToList(),
                DailySales = DailySales.Select(d => new Infrastructure.Printing.Templates.ReportDailySales
                {
                    Date          = d.Date,
                    InvoicesCount = d.InvoicesCount,
                    TotalSales    = d.TotalSales
                }).ToList(),
                CreditCustomers = Credits
                    .GroupBy(c => c.CustomerName)
                    .Select(g => new Infrastructure.Printing.Templates.ReportCreditCustomer
                    {
                        CustomerName    = g.Key,
                        RemainingAmount = g.Sum(x => x.RemainingAmount)
                    }).ToList()
            };
        }
        
        private FlowDocument BuildReportDocument(Infrastructure.Printing.Settings.PrintSettings settings)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Arial"),
                FontSize = 11,
                PagePadding = new Thickness(40),
                FlowDirection = FlowDirection.RightToLeft
            };

            // Header
            var headerPara = new Paragraph(new Run(settings?.ShopName ?? "تقرير المبيعات"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(headerPara);

            if (!string.IsNullOrEmpty(settings?.ShopAddress))
            {
                doc.Blocks.Add(new Paragraph(new Run(settings.ShopAddress))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center
                });
            }

            if (!string.IsNullOrEmpty(settings?.ShopPhone))
            {
                doc.Blocks.Add(new Paragraph(new Run($"هاتف: {settings.ShopPhone}"))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });
            }

            // Period
            doc.Blocks.Add(new Paragraph(new Run($"الفترة: {StartDate:yyyy-MM-dd} إلى {EndDate:yyyy-MM-dd}"))
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Summary Statistics
            doc.Blocks.Add(new Paragraph(new Run("📊 ملخص الإحصائيات"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Blocks.Add(new Paragraph(new Run(
                $"💰 إجمالي المبيعات: {TotalSales:N2} د.م\n" +
                $"📈 إجمالي الأرباح: {TotalProfit:N2} د.م\n" +
                $"📋 عدد الفواتير: {InvoicesCount}\n" +
                $"📊 معدل الفاتورة: {AverageSale:N2} د.م\n" +
                $"💵 هامش الربح: {ProfitMarginPercent:N1}%\n" +
                $"📉 نمو المبيعات: {SalesGrowthFormatted}"))
            {
                FontSize = 11,
                Margin = new Thickness(20, 0, 0, 20),
                LineHeight = 20
            });

            // Credits Summary (if any)
            if (TotalCredits > 0)
            {
                doc.Blocks.Add(new Paragraph(new Run("💳 ملخص الديون"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 10)
                });

                doc.Blocks.Add(new Paragraph(new Run(
                    $"💰 إجمالي الديون: {TotalCredits:N2}  د.م\n" +
                    $"✅ المدفوع: {PaidCredits:N2} د.م\n" +
                    $"⏳ المتبقي: {RemainingCredits:N2} د.م \n" +
                    $"👥 عدد الزبائن بديون: {CreditCustomersCount}\n" +
                    $"📊 نسبة التسديد: {CreditPaymentRateFormatted}"))
                {
                    FontSize = 11,
                    Margin = new Thickness(20, 0, 0, 20),
                    LineHeight = 20
                });
            }

            // Top Products
            if (TopProducts.Any())
            {
                doc.Blocks.Add(new Paragraph(new Run("🏆 أفضل المنتجات مبيعاً"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 10)
                });

                var table = new Table
                {
                    CellSpacing = 0,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };

                table.Columns.Add(new TableColumn { Width = new GridLength(40) }); // Rank
                table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Product
                table.Columns.Add(new TableColumn { Width = new GridLength(80) }); // Quantity
                table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // Sales

                var headerRow = new TableRowGroup();
                var headerTr = new TableRow();
                headerTr.Cells.Add(new TableCell(new Paragraph(new Run("#"))) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                headerTr.Cells.Add(new TableCell(new Paragraph(new Run("المنتج"))) { FontWeight = FontWeights.Bold });
                headerTr.Cells.Add(new TableCell(new Paragraph(new Run("الكمية"))) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
                headerTr.Cells.Add(new TableCell(new Paragraph(new Run("المبيعات"))) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                headerRow.Rows.Add(headerTr);
                table.RowGroups.Add(headerRow);

                var dataRow = new TableRowGroup();
                int rank = 1;
                foreach (var product in TopProducts.Take(10))
                {
                    var tr = new TableRow();
                    tr.Cells.Add(new TableCell(new Paragraph(new Run(rank.ToString()))) { TextAlignment = TextAlignment.Center });
                    tr.Cells.Add(new TableCell(new Paragraph(new Run(product.ProductName))));
                    tr.Cells.Add(new TableCell(new Paragraph(new Run(product.QuantitySold.ToString("N2")))) { TextAlignment = TextAlignment.Center });
                    tr.Cells.Add(new TableCell(new Paragraph(new Run($"{product.TotalRevenue:N2}"))) { TextAlignment = TextAlignment.Right });
                    dataRow.Rows.Add(tr);
                    rank++;
                }
                table.RowGroups.Add(dataRow);

                doc.Blocks.Add(table);
            }

            // Footer
            doc.Blocks.Add(new Paragraph(new Run($"\nطُبع بتاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 30, 0, 0)
            });

            return doc;
        }

        private void ComparePeriods()
        {
            MessageBox.Show(
                "ميزة مقارنة الفترات قيد التطوير",
                "قريباً",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private async Task ReprintInvoiceAsync(SaleReportDto saleDto)
        {
            if (saleDto == null) return;

            try
            {
                using var ctx = new AppDbContext();

                // Load full sale with all relationships
                var sale = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.Sales
                        .Include(s => s.Items)
                            .ThenInclude(i => i.Product)
                        .Include(s => s.Payments)
                        .Include(s => s.Customer)
                        .Where(s => s.SaleNumber == saleDto.SaleNumber));

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

        private async Task ViewInvoiceDetailsAsync(SaleReportDto saleDto)
        {
            if (saleDto == null) return;

            try
            {
                using var ctx = new AppDbContext();

                // Load full sale with all relationships
                var sale = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    ctx.Sales
                        .Include(s => s.Items)
                            .ThenInclude(i => i.Product)
                        .Include(s => s.Payments)
                        .Include(s => s.Customer)
                        .Where(s => s.SaleNumber == saleDto.SaleNumber));

                if (sale == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على الفاتورة!", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Build detailed invoice info
                var details = $"📄 تفاصيل الفاتورة\n";
                details += $"════════════════════════════════\n\n";
                details += $"🔢 رقم الفاتورة: {sale.SaleNumber}\n";
                details += $"📅 التاريخ: {sale.SaleDate:yyyy-MM-dd HH:mm}\n";
                details += $"👤 العميل: {sale.Customer?.FullName ?? "زبون عادي"}\n";
                details += $"💳 طريقة الدفع: {sale.Payments?.FirstOrDefault()?.PaymentMethod ?? PaymentMethod.Cash}\n";
                details += $"👨‍💼 الكاشير: {sale.Cashier ?? "غير محدد"}\n\n";

                details += $"📦 المنتجات:\n";
                details += $"────────────────────────────────\n";

                if (sale.Items != null && sale.Items.Any())
                {
                    foreach (var item in sale.Items)
                    {
                        details += $"• {item.ProductName ?? "منتج"}\n";
                        details += $"  الكمية: {item.Quantity:N2} × {item.UnitPrice:N2} = {item.LineTotal:N2} د.م\n";
                        if (item.DiscountAmount > 0)
                        {
                            details += $"  الخصم: -{item.DiscountAmount:N2} د.م\n";
                        }
                    }
                }

                details += $"\n────────────────────────────────\n";
                details += $"💰 الإجمالي الفرعي: {sale.SubTotal:N2} د.م\n";
                details += $"🎁 الخصم: {sale.DiscountAmount:N2} د.م\n";
                details += $"💵 الضريبة ({sale.TaxRate}%): {sale.TaxAmount:N2} د.م\n";
                details += $"════════════════════════════════\n";
                details += $"✅ المبلغ الإجمالي: {sale.TotalAmount:N2} د.م\n";
                details += $"💵 المدفوع: {sale.PaidAmount:N2} د.م\n";
                details += $"⏳ المتبقي: {sale.RemainingAmount:N2} د.م\n";

                if (!string.IsNullOrEmpty(sale.Notes))
                {
                    details += $"\n📝 ملاحظات: {sale.Notes}\n";
                }

                MessageBox.Show(details, "تفاصيل الفاتورة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في عرض تفاصيل الفاتورة:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PreviewInvoiceAsync(SaleReportDto saleDto)
        {
            if (saleDto == null)
            {
                MessageBox.Show("يرجى تحديد فاتورة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();
                var sale = await ctx.Sales
                    .Include(s => s.Items).ThenInclude(i => i.Product)
                    .Include(s => s.Payments)
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.Id == saleDto.SaleId);

                if (sale == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على الفاتورة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var printerService = PrinterService.Instance;
                var settings = printerService.LoadSettings();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var previewWindow = new Infrastructure.Printing.Views.ProfessionalTemplatePreviewWindow(sale, settings);
                        previewWindow.Show();
                    }
                    catch
                    {
                        // fallback: show details as text
                        _ = ViewInvoiceDetailsAsync(saleDto);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في معاينة الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSaleAsync(SaleReportDto saleDto)
        {
            if (saleDto == null)
            {
                MessageBox.Show("يرجى تحديد فاتورة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف الفاتورة رقم {saleDto.SaleNumber}؟\n⚠️ هذا الإجراء لا يمكن التراجع عنه!",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new AppDbContext();
                var sale = await ctx.Sales
                    .Include(s => s.Items)
                    .Include(s => s.Payments)
                    .FirstOrDefaultAsync(s => s.Id == saleDto.SaleId);

                if (sale == null)
                {
                    MessageBox.Show("❌ لم يتم العثور على الفاتورة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ctx.Sales.Remove(sale);
                await ctx.SaveChangesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Sales.Remove(saleDto);
                    InvoicesCount = Sales.Count;
                    TotalSales = Sales.Sum(s => s.TotalAmount);
                    TotalProfit = Sales.Sum(s => s.TotalProfit);
                    MessageBox.Show($"✅ تم حذف الفاتورة {saleDto.SaleNumber} بنجاح!",
                        "تم الحذف", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في حذف الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        #endregion
    }
    
    #region DTOs

    public class SaleReportDto
    {
        public int SaleId { get; set; } // Added SaleId
        public string SaleNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public int ItemsCount { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalProfit { get; set; }
        public string ProfitMarginFormatted { get; set; }
    }

    public class AdvancedTopProductDto
    {
        public string ProductName { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public string ProfitMarginFormatted => $"{ProfitMargin:N1}%";
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ItemsCount { get; set; }
        public decimal Percentage { get; set; }
        public string PercentageFormatted => $"{Percentage:N1}%";
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public string DateFormatted => Date.ToString("yyyy-MM-dd");
        public string DayName => Date.ToString("dddd", new System.Globalization.CultureInfo("ar-SA"));
        public decimal TotalSales { get; set; }
        public int InvoicesCount { get; set; }
        public decimal AverageSale { get; set; }
    }

    public class HourlySalesDto
    {
        public int Hour { get; set; }
        public string HourFormatted => $"{Hour:00}:00";
        public decimal TotalSales { get; set; }
        public int InvoicesCount { get; set; }
    }

    public class CreditReportDto
    {
        public int CreditId { get; set; }
        public string CustomerName { get; set; }
        public string SaleNumber { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateFormatted => CreatedDate.ToString("yyyy-MM-dd");
        public DateTime? DueDate { get; set; }
        public string DueDateFormatted => DueDate?.ToString("yyyy-MM-dd") ?? "غير محدد";
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public decimal PaymentPercentage => OriginalAmount > 0 ? (PaidAmount / OriginalAmount) * 100 : 0;
    }

    #endregion
}

using System;
using System.Threading.Tasks;
using POS_BIGA.Data;
using POS_BIGA.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Services;
using System.Collections.Generic;

namespace POS_BIGA.Services
{
    /// <summary>
    /// 🛒 خدمة البيع المحدثة - Sale Service
    /// تصميم احترافي يدعم البيع النقدي والكريدي والمختلط
    /// </summary>
    public interface ISaleService
    {
        Task<Sale> CreateSaleAsync(Sale sale, decimal amountPaid, int? customerId = null, PaymentMethod paymentMethod = PaymentMethod.Cash);
        Task<Sale> GetSaleByIdAsync(int saleId);
        Task<List<Sale>> GetSalesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Sale>> GetCustomerSalesAsync(int customerId);

        // للتوافق مع الكود القديم
        Task<int> SaveSaleAsync(Sale sale, decimal amountPaid, PaymentMethod method, int? customerId = null, ICreditService creditService = null);
    }

    public class SaleService : ISaleService
    {
        private readonly IAppStateService _appState;

        public SaleService(IAppStateService appState)
        {
            _appState = appState;
        }

        /// <summary>
        /// 🆕 إنشاء بيع جديد بالتصميم الاحترافي
        /// </summary>
        public async Task<Sale> CreateSaleAsync(Sale sale, decimal amountPaid, int? customerId = null, PaymentMethod paymentMethod = PaymentMethod.Cash)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            if (sale.Items == null || sale.Items.Count == 0)
                throw new InvalidOperationException("لا يمكن حفظ بيع بدون عناصر في السلة.");

            using var ctx = new AppDbContext();

            // استخدام ExecutionStrategy للتوافق مع EnableRetryOnFailure
            var strategy = ctx.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await ctx.Database.BeginTransactionAsync();

                try
                {
                    var stockWarnings = new List<string>();

                    // 1. إنشاء SaleNumber فريد
                    sale.SaleNumber = await GenerateUniqueSaleNumberAsync(ctx);

                    // 2. ربط بالزبون إذا تم تحديده
                    sale.CustomerId = customerId;

                    // 3. تعيين المعلومات الأساسية
                    sale.SaleDate = DateTime.UtcNow;
                    sale.Cashier = sale.Cashier ?? SessionService.CurrentUserDisplayName;
                    sale.RegisterId = sale.RegisterId ?? Environment.MachineName;
                    sale.CreatedBy = SessionService.CurrentUserDisplayName;
                    sale.CreatedAt = DateTime.UtcNow;

                    // 4. تأكد من صحة الحقول المطلوبة
                    sale.Notes = sale.Notes ?? string.Empty;
                    sale.Status = "Completed";

                    // 5. معالجة SaleItems (Snapshot من Product)
                    foreach (var item in sale.Items)
                    {
                        if (item.Quantity <= 0m)
                            throw new InvalidOperationException($"كمية غير صالحة للمنتج {item.ProductId}.");

                        var product = await ctx.Products.FindAsync(item.ProductId);
                        if (product == null)
                            throw new InvalidOperationException($"المنتج {item.ProductId} غير موجود");

                        if (product.CurrentStock < item.Quantity)
                        {
                            stockWarnings.Add($"{product.Name} (متاح: {product.CurrentStock:N3}، مطلوب: {item.Quantity:N3})");
                        }

                        // Snapshot من معلومات المنتج
                        item.ProductName = product.Name;
                        item.ProductBarcode = product.Barcode ?? string.Empty;
                        item.UnitName = product.Unit ?? "pcs";

                        // حساب السطر
                        item.RecalculateLine();

                        // تحديث المخزون
                        product.CurrentStock -= item.Quantity;
                        ctx.Products.Update(product);

                        // تسجيل حركة المخزون
                        var stockMovement = new StockMovement
                        {
                            ProductId = product.Id,
                            MovementType = "Sale",
                            Quantity = -item.Quantity, // سالب للخصم
                            QuantityBefore = product.CurrentStock + item.Quantity,
                            QuantityAfter = product.CurrentStock,
                            ReferenceType = "Sale",
                            MovementDate = DateTime.UtcNow,
                            Reason = $"بيع - فاتورة {sale.SaleNumber}",
                            PerformedBy = SessionService.CurrentUserDisplayName,
                            Notes = string.Empty
                        };
                        ctx.StockMovements.Add(stockMovement);
                    }

                    // 6. الحصول على TaxRate من الإعدادات
                    var taxRateSetting = await ctx.AppSettings
                        .FirstOrDefaultAsync(s => s.SettingKey == "TaxRate");
                    if (taxRateSetting != null && decimal.TryParse(taxRateSetting.SettingValue, out var parsedRate))
                    {
                        sale.TaxRate = parsedRate;
                    }

                    // 7. حساب المجاميع
                    sale.CalculateTotals();

                    // 8. تسجيل المبلغ المدفوع والمتبقي
                    sale.PaidAmount = amountPaid < 0m ? 0m : amountPaid;
                    sale.RemainingAmount = sale.TotalAmount - sale.PaidAmount;

                    // 9. تحديد PaymentType
                    if (sale.RemainingAmount <= 0)
                    {
                        sale.PaymentType = paymentMethod.ToString();
                    }
                    else if (amountPaid <= 0)
                    {
                        sale.PaymentType = "Credit";
                    }
                    else
                    {
                        sale.PaymentType = "Mixed";
                    }

                    // 10. حفظ البيع
                    ctx.Sales.Add(sale);
                    await ctx.SaveChangesAsync();

                    System.Diagnostics.Debug.WriteLine($"✓ تم حفظ البيع #{sale.Id} - {sale.SaleNumber}");
                    System.Diagnostics.Debug.WriteLine($"  الإجمالي: {sale.TotalAmount:N2} | المدفوع: {sale.PaidAmount:N2} | المتبقي: {sale.RemainingAmount:N2}");

                    if (stockWarnings.Count > 0)
                    {
                        var warningMessage = $"⚠️ تم إتمام البيع رغم نفاد/نقص الكمية: {string.Join(" | ", stockWarnings)}";
                        System.Diagnostics.Debug.WriteLine(warningMessage);
                        try { _appState?.NotifyPrintingStatus(warningMessage); } catch { }
                    }

                    // 11. إنشاء Credit إذا كان هناك مبلغ متبقي
                    if (sale.RemainingAmount > 0 && customerId.HasValue)
                    {
                        var credit = new Credit
                        {
                            CustomerId = customerId.Value,
                            SaleId = sale.Id,
                            OriginalAmount = sale.RemainingAmount,
                            PaidAmount = 0m,
                            RemainingAmount = sale.RemainingAmount,
                            Status = CreditStatus.Unpaid,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = SessionService.CurrentUserDisplayName,
                            Notes = amountPaid > 0
                                ? $"دفعة جزئية: {amountPaid:N2} من {sale.TotalAmount:N2} د.م" 
                                : $"بيع بالكريدي - فاتورة {sale.SaleNumber}"
                        };

                        ctx.Credits.Add(credit);
                        await ctx.SaveChangesAsync();

                        System.Diagnostics.Debug.WriteLine($"✓ تم إنشاء دين #{credit.Id} بمبلغ {credit.OriginalAmount:N2} د.م");

                        // إشعار الواجهة
                        try { _appState?.NotifyCreditsChanged(); } catch { }
                    }
                    else if (sale.RemainingAmount > 0 && !customerId.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ تحذير: يوجد مبلغ متبقي لكن لم يتم اختيار زبون!");
                    }

                    // 12. Commit Transaction
                    await tx.CommitAsync();

                    return sale;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    System.Diagnostics.Debug.WriteLine($"❌ خطأ في إنشاء البيع: {ex.Message}");
                    throw;
                }
            });
        }

        /// <summary>
        /// توليد رقم فاتورة فريد
        /// </summary>
        private async Task<string> GenerateUniqueSaleNumberAsync(AppDbContext ctx)
        {
            var date = DateTime.Now;
            var prefix = $"INV-{date:yyyyMMdd}";

            var count = await ctx.Sales
                .Where(s => s.SaleNumber.StartsWith(prefix))
                .CountAsync();

            return $"{prefix}-{(count + 1):D5}";
        }

        /// <summary>
        /// الحصول على بيع بالمعرف
        /// </summary>
        public async Task<Sale> GetSaleByIdAsync(int saleId)
        {
            using var ctx = new AppDbContext();
            return await ctx.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Credits)
                    .ThenInclude(c => c.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId);
        }

        /// <summary>
        /// الحصول على قائمة المبيعات
        /// </summary>
        public async Task<List<Sale>> GetSalesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            using var ctx = new AppDbContext();

            var query = ctx.Sales
                .Include(s => s.Customer)
                .Include(s => s.Items)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate.Value);

            return await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        /// <summary>
        /// الحصول على مبيعات زبون محدد
        /// </summary>
        public async Task<List<Sale>> GetCustomerSalesAsync(int customerId)
        {
            using var ctx = new AppDbContext();
            return await ctx.Sales
                .Include(s => s.Items)
                .Include(s => s.Credits)
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        /// <summary>
        /// 🔄 للتوافق مع الكود القديم - سيتم استبداله تدريجياً
        /// </summary>
        [Obsolete("استخدم CreateSaleAsync بدلاً منه")]
        public async Task<int> SaveSaleAsync(Sale sale, decimal amountPaid, PaymentMethod method, int? customerId = null, ICreditService creditService = null)
        {
            var createdSale = await CreateSaleAsync(sale, amountPaid, customerId, method);
            return createdSale.Id;
        }
    }
}

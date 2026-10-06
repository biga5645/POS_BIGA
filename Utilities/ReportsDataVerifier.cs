using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data;

namespace POS_BIGA.Utilities
{
    /// <summary>
    /// أداة للتحقق من بيانات التقارير وحل مشاكل الأصفار
    /// </summary>
    public static class ReportsDataVerifier
    {
        /// <summary>
        /// التحقق من وجود بيانات مبيعات في قاعدة البيانات
        /// </summary>
        public static async Task<string> VerifyDataAsync()
        {
            try
            {
                using var ctx = new AppDbContext();

                var salesCount = await ctx.Sales.CountAsync();
                var saleItemsCount = await ctx.SaleItems.CountAsync();
                var productsCount = await ctx.Products.CountAsync();

                var report = "📊 تقرير التحقق من البيانات:\n\n";
                report += $"🧾 عدد المبيعات: {salesCount}\n";
                report += $"📦 عدد عناصر المبيعات: {saleItemsCount}\n";
                report += $"🏷️ عدد المنتجات: {productsCount}\n\n";

                if (salesCount == 0)
                {
                    report += "⚠️ تحذير: لا توجد مبيعات في قاعدة البيانات!\n";
                    report += "💡 الحل: قم ببيع بعض المنتجات أولاً.\n\n";
                    return report;
                }

                // Check sales with zero totals
                var salesWithZeroTotal = await ctx.Sales
                    .Include(s => s.Items)
                    .Where(s => s.Total == 0)
                    .ToListAsync();

                if (salesWithZeroTotal.Any())
                {
                    report += $"⚠️ تحذير: {salesWithZeroTotal.Count} مبيعات بقيمة Total = 0\n";
                    report += "🔧 جاري الإصلاح...\n\n";

                    // Fix sales with zero total
                    int fixedCount = 0;
                    foreach (var sale in salesWithZeroTotal)
                    {
                        if (sale.Items.Any())
                        {
                            sale.Total = sale.Items.Sum(i => i.LineTotal);
                            fixedCount++;
                        }
                    }

                    await ctx.SaveChangesAsync();
                    report += $"✅ تم إصلاح {fixedCount} مبيعات\n\n";
                }

                // Calculate actual totals
                var sales = await ctx.Sales
                    .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                    .ToListAsync();

                decimal totalRevenue = 0m;
                decimal totalCost = 0m;

                foreach (var sale in sales)
                {
                    totalRevenue += sale.Total;

                    foreach (var item in sale.Items)
                    {
                        var purchasePrice = item.Product?.PurchasePrice ?? 0m;
                        totalCost += purchasePrice * item.Quantity;
                    }
                }

                decimal profit = totalRevenue - totalCost;
                decimal profitMargin = totalRevenue > 0 ? (profit / totalRevenue) * 100 : 0;

                report += "📈 الإحصائيات الفعلية:\n";
                report += $"💰 إجمالي المبيعات: {totalRevenue:N2} د.م\n";
                report += $"💵 إجمالي التكلفة: {totalCost:N2} د.م\n";
                report += $"📊 إجمالي الأرباح: {profit:N2} د.م\n";
                report += $"📈 هامش الربح: {profitMargin:N1}%\n";

                return report;
            }
            catch (Exception ex)
            {
                return $"❌ خطأ: {ex.Message}\n{ex.StackTrace}";
            }
        }

        /// <summary>
        /// إنشاء بيانات اختبار للتقارير
        /// </summary>
        public static async Task<string> CreateTestDataAsync()
        {
            try
            {
                using var ctx = new AppDbContext();

                // Check if products exist
                var productsCount = await ctx.Products.CountAsync();
                if (productsCount == 0)
                {
                    return "⚠️ لا توجد منتجات في قاعدة البيانات! قم بإضافة منتجات أولاً.";
                }

                // Get first 3 products
                var products = await ctx.Products.Take(3).ToListAsync();

                // Create test sale
                var sale = new Models.Sale
                {
                    CreatedAt = DateTime.Now,
                    Cashier = "مستخدم الاختبار",
                    RegisterId = "REG001"
                };

                // Add items
                foreach (var product in products)
                {
                    var item = new Models.SaleItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Name = product.Name,
                        Quantity = 2,
                        UnitPrice = product.Price
                    };
                    item.RecalculateLine();
                    sale.Items.Add(item);
                }

                // Calculate totals
                sale.TaxRate = 0m;
                sale.CalculateTotals();

                // Add payment
                sale.Payments.Add(new Models.Payment
                {
                    Method = Models.PaymentMethod.Cash,
                    Amount = sale.Total,
                    Reference = "TEST-DATA",
                    PaidAt = DateTime.Now
                });

                ctx.Sales.Add(sale);
                await ctx.SaveChangesAsync();

                return $"✅ تم إنشاء بيانات اختبار بنجاح!\n" +
                       $"🧾 رقم الفاتورة: {sale.Id}\n" +
                       $"💰 الإجمالي: {sale.Total:N2} د.م\n" +
                       $"📦 عدد الأصناف: {sale.Items.Count}";
            }
            catch (Exception ex)
            {
                return $"❌ خطأ: {ex.Message}";
            }
        }
    }
}

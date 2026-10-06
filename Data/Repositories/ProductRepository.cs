using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Infrastructure.Helpers;

namespace POS_BIGA.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        public async Task<List<Product>> GetAllProductsAsync()
        {
            using var _context = new AppDbContext();
            try
            {
                return await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.UnitEntity)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء جلب المنتجات: {ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            using var _context = new AppDbContext();
            try
            {
                // ✅ فحص التكرار قبل الإضافة
                await ThrowIfDuplicateAsync(_context, product, excludeId: 0);

                product.CreatedAt = DateTime.Now;
                product.CreatedBy = POS_BIGA.Services.SessionService.CurrentUserDisplayName;
                product.Description ??= string.Empty;
                product.Barcode ??= string.Empty;
                product.SKU ??= string.Empty;
                product.ImageUrl ??= string.Empty;
                product.UnitEntity = null;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            catch (DuplicateProductException) { throw; }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            using var _context = new AppDbContext();
            try
            {
                var entity = await _context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
                if (entity == null)
                    throw new InvalidOperationException($"Product not found: {product.Id}");

                // ✅ فحص التكرار مع استثناء المنتج الحالي نفسه
                await ThrowIfDuplicateAsync(_context, product, excludeId: product.Id);

                entity.Name = (product.Name ?? string.Empty).Trim();
                entity.Barcode = product.Barcode ?? string.Empty;
                entity.SKU = product.SKU ?? string.Empty;
                entity.Description = product.Description ?? string.Empty;
                entity.ImageUrl = product.ImageUrl ?? string.Empty;
                entity.PurchasePrice = product.PurchasePrice;
                entity.SalePrice = product.SalePrice;
                entity.WholesalePrice = product.WholesalePrice;
                entity.InstallmentPrice = product.InstallmentPrice;
                entity.CurrentStock = product.CurrentStock;
                entity.MinimumStock = product.MinimumStock;
                entity.CategoryId = product.CategoryId;
                entity.UnitId = product.UnitId;
                entity.IsActive = product.IsActive;
                entity.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (DuplicateProductException) { throw; }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحديث المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            using var _context = new AppDbContext();
            try
            {
                var entity = await _context.Products.FindAsync(productId);
                if (entity == null) return true;

                var hasSaleItems = await _context.SaleItems.AnyAsync(si => si.ProductId == productId);
                if (hasSaleItems) return false;

                _context.Products.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حذف المنتج: {ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task<Product> GetByBarcodeAsync(string barcode)
        {
            using var _context = new AppDbContext();
            barcode = BarcodeHelper.FixBarcodeText(barcode ?? string.Empty);
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.UnitEntity)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);
        }

        // ============================================================
        // 🔒 فحص التكرار الداخلي
        // ============================================================

        private static async Task ThrowIfDuplicateAsync(AppDbContext context, Product product, int excludeId)
        {
            var n = (product.Name ?? string.Empty).Trim();
            var b = (product.Barcode ?? string.Empty).Trim();
            var s = (product.SKU ?? string.Empty).Trim();

            // فحص الاسم فقط إذا لم يكن فارغاً
            if (!string.IsNullOrEmpty(n))
            {
                var dupName = await context.Products
                    .AsNoTracking()
                    .Where(p => p.Id != excludeId && p.Name.ToLower() == n.ToLower())
                    .Select(p => new { p.Id, p.Name })
                    .FirstOrDefaultAsync();

                if (dupName != null)
                    throw new DuplicateProductException("الاسم", n);
            }

            // فحص الباركود فقط إذا لم يكن فارغاً
            if (!string.IsNullOrEmpty(b))
            {
                var dupBarcode = await context.Products
                    .AsNoTracking()
                    .Where(p => p.Id != excludeId && p.Barcode.ToLower() == b.ToLower())
                    .Select(p => new { p.Id, p.Barcode })
                    .FirstOrDefaultAsync();

                if (dupBarcode != null)
                    throw new DuplicateProductException("الباركود", b);
            }

            // فحص SKU فقط إذا لم يكن فارغاً
            if (!string.IsNullOrEmpty(s))
            {
                var dupSku = await context.Products
                    .AsNoTracking()
                    .Where(p => p.Id != excludeId && p.SKU.ToLower() == s.ToLower())
                    .Select(p => new { p.Id, p.SKU })
                    .FirstOrDefaultAsync();

                if (dupSku != null)
                    throw new DuplicateProductException("SKU", s);
            }
        }
    }
}
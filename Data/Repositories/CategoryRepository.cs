using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using System.Linq;
using System.Windows;

namespace POS_BIGA.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        // Do not keep a long-lived DbContext instance to avoid concurrency issues.

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            using var _context = new AppDbContext();
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            using var _context = new AppDbContext();
            return await _context.Categories.FindAsync(id);
        }

        public async Task AddCategoryAsync(Category category)
        {
            using var _context = new AppDbContext();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            using var _context = new AppDbContext();
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            using var _context = new AppDbContext();
            var e = await _context.Categories.FindAsync(id);
            if (e != null)
            {
                // Check for dependent products to avoid foreign key constraint errors
                var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                {
                    MessageBox.Show("لا يمكن حذف هذه الفئة لأنها مرتبطة بمنتجات موجودة. قم بنقل أو حذف المنتجات أولاً.", "تعذر الحذف", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _context.Categories.Remove(e);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            using var _context = new AppDbContext();
            return await _context.Categories.AnyAsync(c => c.Name == name);
        }
    }
}

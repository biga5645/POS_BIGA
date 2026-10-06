using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;

namespace POS_BIGA.Data.Repositories
{
    public class CreditRepository : ICreditRepository
    {
        public async Task<List<Credit>> GetAllCreditsAsync()
        {
            using var _context = new AppDbContext();
            return await _context.Credits.Include(c => c.Customer).ToListAsync();
        }

        public async Task<List<Credit>> GetCreditsByCustomerAsync(int customerId)
        {
            using var _context = new AppDbContext();
            return await _context.Credits.Include(c => c.Customer).Where(c => c.CustomerId == customerId).ToListAsync();
        }

        public async Task<Credit> GetByIdAsync(int id)
        {
            using var _context = new AppDbContext();
            return await _context.Credits.Include(c => c.Customer).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddCreditAsync(Credit credit)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"🔍 CreditRepository.AddCreditAsync: بدء إضافة دين");
                System.Diagnostics.Debug.WriteLine($"   CustomerId: {credit.CustomerId}");
                System.Diagnostics.Debug.WriteLine($"   Amount: {credit.Amount:N2}");
                System.Diagnostics.Debug.WriteLine($"   Status: {credit.Status}");
                System.Diagnostics.Debug.WriteLine($"   SaleId: {credit.SaleId?.ToString() ?? "null"}");

                using var _context = new AppDbContext();
                System.Diagnostics.Debug.WriteLine($"✓ DbContext تم إنشاؤه بنجاح");

                _context.Credits.Add(credit);
                System.Diagnostics.Debug.WriteLine($"✓ Credits.Add() نجح - الدين في Pending");

                var changes = await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✓ SaveChangesAsync() نجح - عدد التغييرات: {changes}");
                System.Diagnostics.Debug.WriteLine($"✓ Id بعد الحفظ: {credit.Id}");

                if (credit.Id == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️⚠️ تحذير: Id=0 بعد SaveChangesAsync!");
                    System.Diagnostics.Debug.WriteLine($"⚠️ هذا يعني أن SaveChangesAsync لم يعمل بشكل صحيح");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅✅ نجاح! الدين تم حفظه بـ Id={credit.Id}");
                }

                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌❌ خطأ فادح في CreditRepository.AddCreditAsync:");
                System.Diagnostics.Debug.WriteLine($"   النوع: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   الرسالة: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
                throw;
            }
        }

        public async Task UpdateCreditAsync(Credit credit)
        {
            using var _context = new AppDbContext();
            _context.Credits.Update(credit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCreditAsync(int id)
        {
            using var _context = new AppDbContext();
            var e = await _context.Credits.FindAsync(id);
            if (e != null)
            {
                _context.Credits.Remove(e);
                await _context.SaveChangesAsync();
            }
        }
    }
}

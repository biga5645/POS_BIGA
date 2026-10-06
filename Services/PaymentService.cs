using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data;
using POS_BIGA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS_BIGA.Services
{
    /// <summary>
    /// 💵 خدمة التسديدات - Payment Service
    /// إدارة التسديدات على الديون (Credit-based)
    /// </summary>
    public class PaymentService
    {
        private readonly AppDbContext _context;
        private readonly CreditService _creditService;

        public PaymentService(AppDbContext context, CreditService creditService)
        {
            _context = context;
            _creditService = creditService;
        }

        /// <summary>
        /// إضافة تسديد جديد
        /// </summary>
        public async Task<Payment> AddPaymentAsync(Payment payment)
        {
            try
            {
                // 1. التحقق من وجود Credit
                var credit = await _context.Credits
                    .Include(c => c.Customer)
                    .FirstOrDefaultAsync(c => c.Id == payment.CreditId);

                if (credit == null)
                    throw new InvalidOperationException("الدين غير موجود");

                // 2. التحقق من المبلغ
                if (payment.Amount <= 0)
                    throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

                if (payment.Amount > credit.RemainingAmount)
                    throw new InvalidOperationException($"المبلغ أكبر من المتبقي ({credit.RemainingAmount:N2} د.م)");

                // 3. إضافة Payment
                payment.PaymentDate = DateTime.UtcNow;
                payment.CreatedAt = DateTime.UtcNow;
                _context.Payments.Add(payment);

                // 4. تحديث Credit
                credit.PaidAmount += payment.Amount;
                credit.RemainingAmount -= payment.Amount;
                credit.UpdatedAt = DateTime.UtcNow;
                
                // تحديث الحالة
                credit.UpdateRemainingAmount();
                
                _context.Credits.Update(credit);

                // 5. حفظ التغييرات
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"✓ تم تسديد {payment.Amount:N2} د.م على الدين #{credit.Id} - المتبقي: {credit.RemainingAmount:N2} د.م");

                return payment;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في التسديد: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// الحصول على جميع تسديدات دين معين
        /// </summary>
        public async Task<List<Payment>> GetCreditPaymentsAsync(int creditId)
        {
            return await _context.Payments
                .Where(p => p.CreditId == creditId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        /// <summary>
        /// الحصول على جميع تسديدات زبون
        /// </summary>
        public async Task<List<Payment>> GetCustomerPaymentsAsync(int customerId)
        {
            return await _context.Payments
                .Include(p => p.Credit)
                .Where(p => p.Credit.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        /// <summary>
        /// حذف تسديد (undo)
        /// </summary>
        public async Task<bool> DeletePaymentAsync(int paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Credit)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                    return false;

                // إرجاع المبلغ للدين
                var credit = payment.Credit;
                credit.PaidAmount -= payment.Amount;
                credit.RemainingAmount += payment.Amount;
                credit.UpdateRemainingAmount();
                credit.UpdatedAt = DateTime.UtcNow;

                _context.Payments.Remove(payment);
                _context.Credits.Update(credit);

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"✓ تم حذف تسديد بمبلغ {payment.Amount:N2} د.م");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في حذف التسديد: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// الحصول على إجمالي التسديدات خلال فترة
        /// </summary>
        public async Task<decimal> GetTotalPaymentsAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);
        }

        /// <summary>
        /// الحصول على تسديدات اليوم
        /// </summary>
        public async Task<List<Payment>> GetTodayPaymentsAsync()
        {
            var today = DateTime.Today;
            return await _context.Payments
                .Include(p => p.Credit)
                    .ThenInclude(c => c.Customer)
                .Where(p => p.PaymentDate >= today)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}

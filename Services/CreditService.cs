using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;
using POS_BIGA.Models;
using System.Linq;

namespace POS_BIGA.Services
{
    public interface ICreditService
    {
        Task<List<Credit>> GetAllAsync();
        Task<List<Credit>> GetByCustomerAsync(int customerId);
        Task<Credit> GetByIdAsync(int id);
        Task AddAsync(Credit credit);
        Task UpdateAsync(Credit credit);
        Task DeleteAsync(int id);
    }

    public class CreditService : ICreditService
    {
        private readonly ICreditRepository _repo;
        private readonly IAppStateService _appState;
        private readonly ICustomerService _customerService;

        public CreditService(ICreditRepository repo, IAppStateService appState, ICustomerService customerService)
        {
            _repo = repo;
            _appState = appState;
            _customerService = customerService;
        }

        public async Task<List<Credit>> GetAllAsync() => await _repo.GetAllCreditsAsync();

        public async Task<List<Credit>> GetByCustomerAsync(int customerId) => await _repo.GetCreditsByCustomerAsync(customerId);

        public async Task<Credit> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task AddAsync(Credit credit)
        {
            try
            {
                if (credit == null) return;

                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"📝 CreditService.AddAsync: بدء إضافة دين جديد");
                System.Diagnostics.Debug.WriteLine($"   العميل Id: {credit.CustomerId}");
                System.Diagnostics.Debug.WriteLine($"   المبلغ: {credit.Amount:N2} د.م");
                System.Diagnostics.Debug.WriteLine($"   الحالة: {credit.Status}");

                var customer = await _customerService.GetByIdAsync(credit.CustomerId);
                if (customer != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   اسم العميل: {customer.FullName}");

                    var existingCredits = await _repo.GetCreditsByCustomerAsync(customer.Id);
                    var unpaid = existingCredits.Where(c => c.Status == CreditStatus.Unpaid).Sum(c => c.Amount);

                    System.Diagnostics.Debug.WriteLine($"   الديون الحالية (غير مدفوعة): {unpaid:N2} د.م");
                    System.Diagnostics.Debug.WriteLine($"   عدد الديون الموجودة: {existingCredits.Count}");
                    System.Diagnostics.Debug.WriteLine($"   الديون بعد الإضافة: {(unpaid + credit.Amount):N2} د.م");
                    System.Diagnostics.Debug.WriteLine($"   سقف الدين: {customer.CreditLimit:N2} د.م");

                    if (unpaid + credit.Amount > customer.CreditLimit)
                    {
                        // Informational only: do not block adding, show temporary message
                        MessageBox.Show($"تحذير: مجموع الديون ({(unpaid + credit.Amount):N2} د.م) تجاوز سقف الدين المسموح ({customer.CreditLimit:N2} د.م).", "تحذير", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ تحذير: لم يتم العثور على العميل Id={credit.CustomerId}");
                }

                if (credit.CreatedAt == default) credit.CreatedAt = System.DateTime.UtcNow;

                System.Diagnostics.Debug.WriteLine($"⏳ حفظ الدين في قاعدة البيانات...");
                await _repo.AddCreditAsync(credit);
                System.Diagnostics.Debug.WriteLine($"✓ تم حفظ الدين بنجاح - Id={credit.Id}");

                System.Diagnostics.Debug.WriteLine($"📢 إشعار التطبيق بتحديث الديون...");
                _appState.NotifyCreditsChanged();
                System.Diagnostics.Debug.WriteLine($"✓ تم إرسال إشعار NotifyCreditsChanged");
                System.Diagnostics.Debug.WriteLine($"═══════════════════════════════════════");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في CreditService.AddAsync: {ex}");
                MessageBox.Show($"خطأ أثناء إضافة الدين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task UpdateAsync(Credit credit)
        {
            try
            {
                await _repo.UpdateCreditAsync(credit);
                _appState.NotifyCreditsChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ????? ????? ?????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await _repo.DeleteCreditAsync(id);
                _appState.NotifyCreditsChanged();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"??? ????? ??? ?????: {ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

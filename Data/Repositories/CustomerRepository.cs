using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using System.Linq;

namespace POS_BIGA.Data.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            using var _context = new AppDbContext();

            // جلب العملاء مع الديون المرتبطة بهم
            var customers = await _context.Customers
                .Include(c => c.Credits)
                .ToListAsync();

            // حساب الديون الحالية لكل عميل
            foreach (var customer in customers)
            {
                var unpaidCredits = customer.Credits?.Where(c => c.Status == CreditStatus.Unpaid).ToList() ?? new List<Credit>();
                customer.CurrentDebt = unpaidCredits.Sum(c => c.Amount);
                // LastCreditDate يُحسب تلقائياً من Credits
            }

            return customers;
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            using var _context = new AppDbContext();

            // جلب العميل مع الديون المرتبطة به
            var customer = await _context.Customers
                .Include(c => c.Credits)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer != null)
            {
                // حساب الديون الحالية
                var unpaidCredits = customer.Credits?.Where(c => c.Status == CreditStatus.Unpaid).ToList() ?? new List<Credit>();
                customer.CurrentDebt = unpaidCredits.Sum(c => c.Amount);
                // LastCreditDate يُحسب تلقائياً من Credits
            }

            return customer;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            using var _context = new AppDbContext();
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            using var _context = new AppDbContext();
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(int id)
        {
            using var _context = new AppDbContext();
            var e = await _context.Customers.FindAsync(id);
            if (e != null)
            {
                _context.Customers.Remove(e);
                await _context.SaveChangesAsync();
            }
        }
    }
}

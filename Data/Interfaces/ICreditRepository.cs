using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Interfaces
{
    public interface ICreditRepository
    {
        Task<List<Credit>> GetAllCreditsAsync();
        Task<List<Credit>> GetCreditsByCustomerAsync(int customerId);
        Task<Credit> GetByIdAsync(int id);
        Task AddCreditAsync(Credit credit);
        Task UpdateCreditAsync(Credit credit);
        Task DeleteCreditAsync(int id);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Services
{
    public interface IUnitService
    {
        Task<List<Unit>> GetAllAsync();
        Task<Unit> GetByIdAsync(int id);
        Task AddAsync(Unit unit);
        Task UpdateAsync(Unit unit);
        Task DeleteAsync(int id);
        Task<bool> ExistsByAbbreviationAsync(string abbreviation);
    }
}

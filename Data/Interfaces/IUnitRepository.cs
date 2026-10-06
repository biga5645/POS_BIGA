using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Interfaces
{
    public interface IUnitRepository
    {
        Task<List<Unit>> GetAllUnitsAsync();
        Task<Unit> GetByIdAsync(int id);
        Task AddUnitAsync(Unit unit);
        Task UpdateUnitAsync(Unit unit);
        Task DeleteUnitAsync(int id);
        Task<bool> ExistsByAbbreviationAsync(string abbreviation);
    }
}

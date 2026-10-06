using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;

namespace POS_BIGA.Services
{
    public class UnitService : IUnitService
    {
        private readonly IUnitRepository _repo;

        public UnitService() : this(new UnitRepository()) { }

        public UnitService(IUnitRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Unit>> GetAllAsync() => await _repo.GetAllUnitsAsync();

        public async Task<Unit> GetByIdAsync(int id) => await _repo.GetByIdAsync(id);

        public async Task AddAsync(Unit unit) => await _repo.AddUnitAsync(unit);

        public async Task UpdateAsync(Unit unit) => await _repo.UpdateUnitAsync(unit);

        public async Task DeleteAsync(int id) => await _repo.DeleteUnitAsync(id);

        public async Task<bool> ExistsByAbbreviationAsync(string abbreviation) => await _repo.ExistsByAbbreviationAsync(abbreviation);
    }
}

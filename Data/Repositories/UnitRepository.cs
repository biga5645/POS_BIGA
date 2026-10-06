using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using System.Linq;

namespace POS_BIGA.Data.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        public async Task<List<Unit>> GetAllUnitsAsync()
        {
            using var _context = new AppDbContext();
            return await _context.Units.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task<Unit> GetByIdAsync(int id)
        {
            using var _context = new AppDbContext();
            return await _context.Units.FindAsync(id);
        }

        public async Task AddUnitAsync(Unit unit)
        {
            using var _context = new AppDbContext();
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUnitAsync(Unit unit)
        {
            using var _context = new AppDbContext();
            var entity = await _context.Units.FirstOrDefaultAsync(u => u.Id == unit.Id);
            if (entity == null) return;

            entity.Name = unit.Name;
            entity.Abbreviation = unit.Abbreviation;
            entity.IsActive = unit.IsActive;
            entity.IsDivisible = unit.IsDivisible;
            entity.SubUnitName = unit.SubUnitName;
            entity.SubUnitQuantity = unit.SubUnitQuantity;
            entity.AllowCustomPrice = unit.AllowCustomPrice;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteUnitAsync(int id)
        {
            using var _context = new AppDbContext();
            var e = await _context.Units.FindAsync(id);
            if (e != null)
            {
                _context.Units.Remove(e);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByAbbreviationAsync(string abbreviation)
        {
            using var _context = new AppDbContext();
            return await _context.Units.AnyAsync(u => u.Abbreviation == abbreviation);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Interfaces
{
    public interface IUserRepository
    {
        Task<List<AppUser>> GetAllAsync();
        Task<AppUser?> GetByIdAsync(int id);
        Task<AppUser?> GetByUsernameAsync(string username);
        Task AddAsync(AppUser user);
        Task UpdateAsync(AppUser user);
        Task UpdatePasswordAsync(int userId, string passwordHash);
        Task UpdatePinAsync(int userId, string? pinHash);
        Task DeleteAsync(int id);
        Task UpdateLastLoginAsync(int userId);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Interfaces
{
    public interface IAppSettingRepository
    {
        Task<List<AppSetting>> GetAllAsync();
        Task<AppSetting> GetByKeyAsync(string key);
        Task<AppSetting> GetStoreSettingsAsync();
        Task AddAsync(AppSetting setting);
        Task UpdateAsync(AppSetting setting);
        Task DeleteAsync(int id);
        Task SetValueAsync(string key, string value, string description = null);
        Task SaveStoreSettingsAsync(AppSetting settings);
    }
}
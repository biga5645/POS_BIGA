using System.Threading.Tasks;
using POS_BIGA.Models;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Data.Repositories;
using System.Collections.Generic;

namespace POS_BIGA.Services
{
    public interface IAppSettingService
    {
        Task<AppSetting> GetStoreSettingsAsync();
        Task SaveStoreSettingsAsync(AppSetting settings);
        Task<List<AppSetting>> GetAllAsync();
        Task<AppSetting> GetByKeyAsync(string key);
        Task SetValueAsync(string key, string value, string description = null);
    }

    public class AppSettingService : IAppSettingService
    {
        private readonly IAppSettingRepository _repo;

        public AppSettingService() : this(new AppSettingRepository()) { }

        public AppSettingService(IAppSettingRepository repo)
        {
            _repo = repo;
        }

        public async Task<AppSetting> GetStoreSettingsAsync() => await _repo.GetStoreSettingsAsync();

        public async Task SaveStoreSettingsAsync(AppSetting settings) => await _repo.SaveStoreSettingsAsync(settings);

        public async Task<List<AppSetting>> GetAllAsync() => await _repo.GetAllAsync();

        public async Task<AppSetting> GetByKeyAsync(string key) => await _repo.GetByKeyAsync(key);

        public async Task SetValueAsync(string key, string value, string description = null) => await _repo.SetValueAsync(key, value, description);
    }
}
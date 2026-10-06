using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Repositories
{
    public class AppSettingRepository : IAppSettingRepository
    {
        public async Task<List<AppSetting>> GetAllAsync()
        {
            using var ctx = new AppDbContext();
            return await ctx.AppSettings.ToListAsync();
        }

        public async Task<AppSetting> GetByKeyAsync(string key)
        {
            using var ctx = new AppDbContext();
            return await ctx.AppSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task<AppSetting> GetStoreSettingsAsync()
        {
            using var ctx = new AppDbContext();
            return await ctx.AppSettings.FirstOrDefaultAsync();
        }

        public async Task AddAsync(AppSetting setting)
        {
            using var ctx = new AppDbContext();
            ctx.AppSettings.Add(setting);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(AppSetting setting)
        {
            using var ctx = new AppDbContext();
            var existing = await ctx.AppSettings.FindAsync(setting.Id);
            if (existing == null) return;

            existing.SettingKey   = setting.SettingKey;
            existing.SettingValue = setting.SettingValue;
            existing.SettingType  = setting.SettingType;
            existing.Description  = setting.Description;
            existing.UpdatedAt    = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var ctx = new AppDbContext();
            var entity = await ctx.AppSettings.FindAsync(id);
            if (entity != null)
            {
                ctx.AppSettings.Remove(entity);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task SetValueAsync(string key, string value, string description = null)
        {
            using var ctx = new AppDbContext();

            var existing = await ctx.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (existing == null)
            {
                ctx.AppSettings.Add(new AppSetting
                {
                    SettingKey   = key,
                    SettingValue = value ?? string.Empty,
                    SettingType  = "String",
                    Description  = description ?? string.Empty,
                    Category     = "General",
                    CreatedAt    = DateTime.UtcNow,
                    IsEditable   = true,
                });
            }
            else
            {
                existing.SettingValue = value ?? string.Empty;
                if (!string.IsNullOrEmpty(description))
                    existing.Description = description;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task SaveStoreSettingsAsync(AppSetting settings)
        {
            using var ctx = new AppDbContext();
            await ctx.SaveStoreSettingsAsync(settings);
        }
    }
}
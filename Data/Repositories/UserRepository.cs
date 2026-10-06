using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Models;

namespace POS_BIGA.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        public async Task<List<AppUser>> GetAllAsync()
        {
            using var ctx = new AppDbContext();
            return await ctx.Users.ToListAsync();
        }

        public async Task<AppUser?> GetByIdAsync(int id)
        {
            using var ctx = new AppDbContext();
            return await ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<AppUser?> GetByUsernameAsync(string username)
        {
            using var ctx = new AppDbContext();
            return await ctx.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddAsync(AppUser user)
        {
            using var ctx = new AppDbContext();
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateAsync(AppUser user)
        {
            using var ctx = new AppDbContext();
            // ??? ?????? ?? ????? ???????? ????? ?????? ??? PasswordHash ??????
            var existing = await ctx.Users.FindAsync(user.Id);
            if (existing == null) return;

            // ????? ?????? ??????? ??????? ??? - ?? ???? PasswordHash ???
            existing.Username  = user.Username;
            existing.FullName  = user.FullName;
            existing.Role      = user.Role;
            existing.IsActive  = user.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();

            // ????? ?????? ?????? ?? ??????? ?????? ????????
            user.PasswordHash = existing.PasswordHash;
            user.Pin          = existing.Pin;
            user.UpdatedAt    = existing.UpdatedAt;
        }

        public async Task DeleteAsync(int id)
        {
            using var ctx = new AppDbContext();
            var user = await ctx.Users.FindAsync(id);
            if (user != null)
            {
                ctx.Users.Remove(user);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            using var ctx = new AppDbContext();
            var user = await ctx.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }
        }

        public async Task UpdatePasswordAsync(int userId, string passwordHash)
        {
            using var ctx = new AppDbContext();
            var user = await ctx.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = passwordHash;
                user.UpdatedAt    = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }
        }

        public async Task UpdatePinAsync(int userId, string? pinHash)
        {
            using var ctx = new AppDbContext();
            var user = await ctx.Users.FindAsync(userId);
            if (user != null)
            {
                user.Pin       = pinHash;
                user.UpdatedAt = DateTime.UtcNow;
                await ctx.SaveChangesAsync();
            }
        }
    }
}

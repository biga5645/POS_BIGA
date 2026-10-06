using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POS_BIGA.Data.Interfaces;
using POS_BIGA.Models;
using BC = BCrypt.Net.BCrypt;

namespace POS_BIGA.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // ======================================
        // 📋 القراءة
        // ======================================

        public Task<List<AppUser>> GetAllAsync() => _repo.GetAllAsync();

        public Task<AppUser?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public Task<AppUser?> GetByUsernameAsync(string username) => _repo.GetByUsernameAsync(username);

        public async Task<bool> HasUsersAsync()
        {
            var users = await _repo.GetAllAsync();
            return users.Count > 0;
        }

        // ======================================
        // ✅ الإضافة
        // ======================================

        public async Task<(bool Success, string Error)> AddAsync(AppUser user, string plainPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Username))
                    return (false, "اسم المستخدم مطلوب.");

                if (string.IsNullOrWhiteSpace(plainPassword) || plainPassword.Length < 4)
                    return (false, " كلمة المرور يجب أن تكون 4 أحرف على الأقل.");

                // التحقق من عدم تكرار اسم المستخدم
                var existing = await _repo.GetByUsernameAsync(user.Username.Trim());
                if (existing != null)
                    return (false, $"اسم المستخدم '{user.Username}' مستخدم مسبقاً.");

                user.Username     = user.Username.Trim();
                user.PasswordHash = BC.HashPassword(plainPassword);
                user.CreatedAt    = DateTime.UtcNow;
                user.CreatedBy    = SessionService.CurrentUser?.Username ?? " System";

                // تشفير PIN إذا موجود
                if (!string.IsNullOrWhiteSpace(user.Pin))
                    user.Pin = BC.HashPassword(user.Pin.Trim());

                await _repo.AddAsync(user);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ======================================
        // ✏️ التعديل
        // ======================================

        public async Task<(bool Success, string Error)> UpdateAsync(AppUser user)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(user.Username))
                    return (false, " اسم المستخدم مطلوب.");

                // التحقق من عدم تكرار اسم المستخدم مع مستخدم آخر
                var existing = await _repo.GetByUsernameAsync(user.Username.Trim());
                if (existing != null && existing.Id != user.Id)
                    return (false, $"اسم المستخدم '{user.Username}' مستخدم مسبقاً.");

                user.Username = user.Username.Trim();
                await _repo.UpdateAsync(user);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Error)> ChangePasswordAsync(int userId, string newPlainPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPlainPassword) || newPlainPassword.Length < 4)
                    return (false, " كلمة المرور يجب أن تكون 4 أحرف على الأقل .");

                var user = await _repo.GetByIdAsync(userId);
                if (user == null)
                    return (false, "المستخدم غير موجود .");

                // استخدام UpdatePasswordAsync المباشرة لضمان عدم تلف الـ Hash
                var newHash = BC.HashPassword(newPlainPassword);
                await _repo.UpdatePasswordAsync(userId, newHash);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Error)> ChangePinAsync(int userId, string? newPin)
        {
            try
            {
                // مسح PIN (إفراغه)
                if (string.IsNullOrWhiteSpace(newPin))
                {
                    await _repo.UpdatePinAsync(userId, null);
                    return (true, string.Empty);
                }

                var pin = newPin.Trim();

                // ✅ التحقق: 6-8 أرقام فقط
                if (pin.Length < 6 || pin.Length > 8 || !pin.All(char.IsDigit))
                    return (false, "رمز PIN يجب أن يتكون من 6 إلى 8 أرقام فقط.");

                // ✅ التحقق من عدم تكرار نفس PIN مع مستخدم آخر
                var allUsers = await _repo.GetAllAsync();
                foreach (var u in allUsers.Where(u => u.Id != userId && !string.IsNullOrWhiteSpace(u.Pin)))
                {
                    if (BC.Verify(pin, u.Pin!))
                        return (false, "رمز PIN مستخدم من قِبل مستخدم آخر. اختر رمزاً مختلفاً.");
                }

                var pinHash = BC.HashPassword(pin);
                await _repo.UpdatePinAsync(userId, pinHash);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ======================================
        // 🗑️ الحذف
        // ======================================

        public async Task<(bool Success, string Error)> DeleteAsync(int id)
        {
            try
            {
                var user = await _repo.GetByIdAsync(id);
                if (user == null)
                    return (false, "المستخدم غير موجود .");

                // منع حذف المستخدم الحالي المسجّل دخوله
                if (SessionService.CurrentUser?.Id == id)
                    return (false, "لا يمكن حذف حسابك الخاص وأنت مسجّل الدخول .");

                // منع حذف آخر Admin في النظام (نشط أو غير نشط)
                if (user.Role == UserRole.Admin)
                {
                    var allUsers = await _repo.GetAllAsync();
                    var adminCount = allUsers.Count(u => u.Role == UserRole.Admin);
                    if (adminCount <= 1)
                        return (false, " لا يمكن حذف آخر مدير في النظام. أضف مديراً آخر أولاً.");
                }

                await _repo.DeleteAsync(id);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ======================================
        // 🔐 تسجيل الدخول
        // ======================================

        public async Task<AppUser?> LoginAsync(string username, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(plainPassword))
                return null;

            var user = await _repo.GetByUsernameAsync(username.Trim());
            if (user == null || !user.IsActive)
                return null;

            System.Diagnostics.Debug.WriteLine($"[Login] Hash={user.PasswordHash} Len={user.PasswordHash?.Length} PwdLen={plainPassword.Length}");

            bool valid = BC.Verify(plainPassword, user.PasswordHash);
            System.Diagnostics.Debug.WriteLine($"[Login] BCrypt.Verify={valid}");

            if (!valid) return null;

            await _repo.UpdateLastLoginAsync(user.Id);
            user.LastLoginAt = DateTime.UtcNow;
            return user;
        }

        public async Task<AppUser?> LoginByPinAsync(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin)) return null;

            var users = await _repo.GetAllAsync();
            foreach (var user in users.Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Pin)))
            {
                if (BC.Verify(pin, user.Pin!))
                {
                    await _repo.UpdateLastLoginAsync(user.Id);
                    user.LastLoginAt = DateTime.UtcNow;
                    return user;
                }
            }
            return null;
        }
    }
}

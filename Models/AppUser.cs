using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// نموذج مستخدم النظام
    /// </summary>
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>رمز PIN السريع (4-8 أرقام) - مُخزَّن كـ BCrypt hash</summary>
        [MaxLength(100)]
        public string? Pin { get; set; }

        public UserRole Role { get; set; } = UserRole.Cashier;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // ======================================
        // 🧮 Computed Properties
        // ======================================

        [NotMapped]
        public string RoleDisplayName => Role switch
        {
            UserRole.Admin     => "مدير النظام",
            UserRole.Cashier   => "كاشير",
            UserRole.Inventory => "مسؤول المخزن",
            UserRole.Viewer    => "مشاهد ",
            _                  => "غير محدد"
        };

        [NotMapped]
        public bool HasPin => !string.IsNullOrWhiteSpace(Pin);
    }

    /// <summary>
    /// أدوار المستخدمين في النظام
    /// </summary>
    public enum UserRole
    {
        Admin     = 1,
        Cashier   = 2,
        Inventory = 3,
        Viewer    = 4
    }
}

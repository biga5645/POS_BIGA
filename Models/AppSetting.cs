using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// ⚙️ نموذج الإعدادات - Application Settings Model
    /// إعدادات مرنة بنظام Key-Value
    /// </summary>
    public class AppSetting
    {
        [Key]
        public int Id { get; set; }

        // ======================================
        // 🔑 نظام Key-Value
        // ======================================

        /// <summary>مفتاح الإعداد (فريد)</summary>
        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; }

        /// <summary>قيمة الإعداد</summary>
        [MaxLength(4000)]
        public string SettingValue { get; set; }

        /// <summary>نوع القيمة: String, Int, Decimal, Bool, Date</summary>
        [MaxLength(50)]
        public string SettingType { get; set; }

        /// <summary>وصف الإعداد</summary>
        [MaxLength(500)]
        public string Description { get; set; }

        /// <summary>التصنيف: General, Sales, Inventory, Reports, etc.</summary>
        [MaxLength(100)]
        public string Category { get; set; }

        /// <summary>هل يمكن تعديله من الواجهة؟</summary>
        public bool IsEditable { get; set; } = true;

        // ======================================
        // 🏪 إعدادات المتجر (Strongly-typed)
        // ======================================

        /// <summary>اسم المتجر</summary>
        public string ShopName { get; set; } = string.Empty;

        /// <summary>عنوان المتجر</summary>
        public string ShopAddress { get; set; } = string.Empty;

        /// <summary>هاتف المتجر</summary>
        public string ShopPhone { get; set; } = string.Empty;

        /// <summary>نسبة الضريبة الافتراضية (%)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0m;

        /// <summary>العملة (MAD, EUR, USD)</summary>
        public string Currency { get; set; } = "MAD";

        /// <summary>تفعيل/تعطيل البيع بالكريدي</summary>
        public bool CreditEnabled { get; set; } = false;

        /// <summary>سقف الكريدي الافتراضي للزبائن الجدد</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } = 0m;

        /// <summary>رسالة تظهر في الفاتورة/الإيصال</summary>
        public string InvoiceMessage { get; set; } = string.Empty;

        // ======================================
        // 📅 Audit Trail
        // ======================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ======================================
        // 🔑 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق مع الكود القديم</summary>
        [NotMapped]
        public string Key
        {
            get => SettingKey;
            set => SettingKey = value;
        }

        /// <summary>للحفاظ على التوافق مع الكود القديم</summary>
        [NotMapped]
        public string Value
        {
            get => SettingValue;
            set => SettingValue = value;
        }
    }
}

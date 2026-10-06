using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📦 نموذج المنتج - Product Model
    /// تصميم احترافي مع تتبع المخزون
    /// </summary>
    public class Product : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        // ======================================
        // 📝 المعلومات الأساسية
        // ======================================

        /// <summary>اسم المنتج</summary>
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        /// <summary>الباركود</summary>
        [MaxLength(100)]
        public string Barcode { get; set; } = string.Empty;

        /// <summary>SKU - رمز المنتج الفريد</summary>
        [MaxLength(100)]
        public string SKU { get; set; } = string.Empty;

        /// <summary>الوصف</summary>
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        private string _imageUrl = string.Empty;
        /// <summary>صورة المنتج</summary>
        [MaxLength(500)]
        public string ImageUrl
        {
            get => _imageUrl;
            set
            {
                if (_imageUrl != value)
                {
                    _imageUrl = value ?? string.Empty;
                    OnPropertyChanged(nameof(ImageUrl));
                }
            }
        }

        // ======================================
        // 💰 الأسعار
        // ======================================

        /// <summary>سعر الشراء (التكلفة)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        /// <summary>سعر البيع بالتجزئة</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal SalePrice { get; set; }

        /// <summary>سعر البيع بالجملة (اختياري)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? WholesalePrice { get; set; }

        /// <summary>سعر البيع بالتقسيط (اختياري)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? InstallmentPrice { get; set; }

        // للحفاظ على التوافق مع الكود الحالي
        [NotMapped]
        public decimal Price
        {
            get => SalePrice;
            set => SalePrice = value;
        }

        // ======================================
        // 📦 المخزون
        // ======================================

        /// <summary>الكمية الحالية في المخزون</summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal CurrentStock { get; set; }

        // للحفاظ على التوافق مع الكود الحالي
        [NotMapped]
        public decimal StockQuantity
        {
            get => CurrentStock;
            set => CurrentStock = value;
        }

        /// <summary>الحد الأدنى للمخزون (تنبيه)</summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal MinimumStock { get; set; }

        // ======================================
        // 🔗 العلاقات
        // ======================================

        /// <summary>الفئة</summary>
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        /// <summary>الوحدة</summary>
        public int? UnitId { get; set; }

        [ForeignKey("UnitId")]
        public virtual Unit UnitEntity { get; set; }

        // ✅ للحفاظ على التوافق - نص الوحدة (NotMapped - لا يُحفظ في قاعدة البيانات)
        [NotMapped]
        public string Unit => UnitEntity?.Abbreviation ?? "pcs";

        [NotMapped]
        public string CategoryName => Category?.Name ?? "غير مصنف";

        [NotMapped]
        public decimal StockValue => CurrentStock * SalePrice;

        // ======================================
        // ✅ حالة النشاط
        // ======================================

        public bool IsActive { get; set; } = true;

        // ======================================
        // 📅 Audit Trail
        // ======================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        // ======================================
        // 🔗 Navigation Properties
        // ======================================

        /// <summary>حركات المخزون</summary>
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

        // ======================================
        // 🧮 Computed Properties
        // ======================================

        /// <summary>هل المخزون منخفض؟</summary>
        [NotMapped]
        public bool IsLowStock => CurrentStock <= MinimumStock;

        /// <summary>هل المخزون نفد؟</summary>
        [NotMapped]
        public bool IsOutOfStock => CurrentStock <= 0;

        /// <summary>هامش الربح</summary>
        [NotMapped]
        public decimal ProfitMargin => PurchasePrice > 0 ? ((SalePrice - PurchasePrice) / PurchasePrice) * 100 : 0;

        /// <summary>الربح لكل وحدة</summary>
        [NotMapped]
        public decimal ProfitPerUnit => SalePrice - PurchasePrice;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
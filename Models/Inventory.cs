using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📦 نموذج المخزون - Inventory Model
    /// تتبع الكميات والحركات والتنبيهات
    /// </summary>
    public class Inventory : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        // ======================================
        // 🔗 العلاقات
        // ======================================

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        // ======================================
        // 📊 كميات المخزون
        // ======================================

        private decimal _quantityOnHand;
        /// <summary>الكمية الموجودة حالياً</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuantityOnHand
        {
            get => _quantityOnHand;
            set { if (_quantityOnHand != value) { _quantityOnHand = value; OnPropertyChanged(nameof(QuantityOnHand)); } }
        }

        private decimal _quantityReserved;
        /// <summary>الكمية المحجوزة (المباعة لكن لم تُسلم)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuantityReserved
        {
            get => _quantityReserved;
            set { if (_quantityReserved != value) { _quantityReserved = value; OnPropertyChanged(nameof(QuantityReserved)); } }
        }

        private decimal _quantityAvailable;
        /// <summary>الكمية المتاحة للبيع (الموجودة - المحجوزة)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal QuantityAvailable
        {
            get => _quantityAvailable;
            set { if (_quantityAvailable != value) { _quantityAvailable = value; OnPropertyChanged(nameof(QuantityAvailable)); } }
        }

        // ======================================
        // 🚨 التنبيهات والحدود
        // ======================================

        private decimal _minimumQuantity;
        /// <summary>الحد الأدنى للمخزون - التنبيه عند انخفاض المخزون</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumQuantity
        {
            get => _minimumQuantity;
            set { if (_minimumQuantity != value) { _minimumQuantity = value; OnPropertyChanged(nameof(MinimumQuantity)); } }
        }

        private decimal _maximumQuantity;
        /// <summary>الحد الأقصى للمخزون - الكمية المثالية</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumQuantity
        {
            get => _maximumQuantity;
            set { if (_maximumQuantity != value) { _maximumQuantity = value; OnPropertyChanged(nameof(MaximumQuantity)); } }
        }

        private decimal _reorderQuantity;
        /// <summary>كمية إعادة الطلب - الكمية المقترحة عند الشراء</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReorderQuantity
        {
            get => _reorderQuantity;
            set { if (_reorderQuantity != value) { _reorderQuantity = value; OnPropertyChanged(nameof(ReorderQuantity)); } }
        }

        // ======================================
        // 📅 التواريخ والحالة
        // ======================================

        /// <summary>تاريخ آخر استقبال أو تحديث</summary>
        public DateTime LastReceivedDate { get; set; }

        /// <summary>تاريخ آخر عملية بيع</summary>
        public DateTime? LastSoldDate { get; set; }

        private bool _isLowStock;
        /// <summary>هل المخزون منخفض؟</summary>
        public bool IsLowStock
        {
            get => _isLowStock;
            set { if (_isLowStock != value) { _isLowStock = value; OnPropertyChanged(nameof(IsLowStock)); } }
        }

        private bool _isOutOfStock;
        /// <summary>هل المنتج غير متوفر؟</summary>
        public bool IsOutOfStock
        {
            get => _isOutOfStock;
            set { if (_isOutOfStock != value) { _isOutOfStock = value; OnPropertyChanged(nameof(IsOutOfStock)); } }
        }

        /// <summary>تاريخ الإنشاء</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>تاريخ آخر تحديث</summary>
        public DateTime? UpdatedAt { get; set; }

        // ======================================
        // 🔍 دوال مساعدة
        // ======================================

        /// <summary>
        /// حساب الكمية المتاحة
        /// </summary>
        public void UpdateAvailableQuantity()
        {
            QuantityAvailable = Math.Max(0, QuantityOnHand - QuantityReserved);
            IsOutOfStock = QuantityAvailable <= 0;
            IsLowStock = QuantityAvailable <= MinimumQuantity && QuantityAvailable > 0;
            OnPropertyChanged(nameof(QuantityAvailable));
            OnPropertyChanged(nameof(IsOutOfStock));
            OnPropertyChanged(nameof(IsLowStock));
        }

        /// <summary>
        /// إضافة كمية للمخزون
        /// </summary>
        public void AddQuantity(decimal quantity)
        {
            if (quantity > 0)
            {
                QuantityOnHand += quantity;
                LastReceivedDate = DateTime.UtcNow;
                UpdateAvailableQuantity();
            }
        }

        /// <summary>
        /// خصم كمية من المخزون
        /// </summary>
        public bool RemoveQuantity(decimal quantity)
        {
            if (quantity > 0 && quantity <= QuantityAvailable)
            {
                QuantityOnHand -= quantity;
                LastSoldDate = DateTime.UtcNow;
                UpdateAvailableQuantity();
                return true;
            }
            return false;
        }

        /// <summary>
        /// حجز كمية (للمبيعات المعلقة)
        /// </summary>
        public bool ReserveQuantity(decimal quantity)
        {
            if (quantity > 0 && quantity <= QuantityAvailable)
            {
                QuantityReserved += quantity;
                UpdateAvailableQuantity();
                return true;
            }
            return false;
        }

        /// <summary>
        /// إلغاء حجز كمية
        /// </summary>
        public void UnreserveQuantity(decimal quantity)
        {
            if (quantity > 0)
            {
                QuantityReserved = Math.Max(0, QuantityReserved - quantity);
                UpdateAvailableQuantity();
            }
        }

        public override string ToString() => $"{Product?.Name} - الكمية: {QuantityOnHand}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

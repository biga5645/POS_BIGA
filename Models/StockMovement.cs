using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📊 نموذج حركة المخزون - Stock Movement Model
    /// تتبع دقيق لكل حركة على المخزون
    /// </summary>
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        // ======================================
        // 🔗 العلاقات
        // ======================================

        /// <summary>رقم المنتج</summary>
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        /// <summary>رقم البيع المرتبط (اختياري)</summary>
        public int? SaleId { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale RelatedSale { get; set; }

        // ======================================
        // 📝 معلومات الحركة
        // ======================================

        /// <summary>
        /// نوع الحركة: Sale, Purchase, Adjustment, Return, Damage, Transfer
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string MovementType { get; set; }

        /// <summary>الكمية (+ للإضافة، - للخصم)</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Required]
        public decimal Quantity { get; set; }

        /// <summary>
        /// نوع المرجع: Sale, Purchase, Adjustment, etc.
        /// </summary>
        [MaxLength(50)]
        public string ReferenceType { get; set; }

        /// <summary>رقم المرجع</summary>
        public int? ReferenceId { get; set; }

        /// <summary>الكمية قبل الحركة</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Required]
        public decimal QuantityBefore { get; set; }

        /// <summary>الكمية بعد الحركة</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Required]
        public decimal QuantityAfter { get; set; }

        /// <summary>تاريخ الحركة</summary>
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;

        /// <summary>السبب/التفصيل</summary>
        [MaxLength(500)]
        public string Reason { get; set; }

        /// <summary>من قام بالحركة</summary>
        [MaxLength(100)]
        public string PerformedBy { get; set; }

        /// <summary>ملاحظات إضافية</summary>
        [MaxLength(500)]
        public string Notes { get; set; }

        // ======================================
        // 🧮 Computed Properties
        // ======================================

        /// <summary>هل الحركة إضافة للمخزون؟</summary>
        [NotMapped]
        public bool IsAddition => Quantity > 0;

        /// <summary>هل الحركة خصم من المخزون؟</summary>
        [NotMapped]
        public bool IsDeduction => Quantity < 0;

        // ======================================
        // 🔄 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق - استخدم MovementDate</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use MovementDate instead")]
        public DateTime Date
        {
            get => MovementDate;
            set => MovementDate = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم MovementType</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use MovementType instead")]
        public string Type
        {
            get => MovementType;
            set => MovementType = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم PerformedBy</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use PerformedBy instead")]
        public string User
        {
            get => PerformedBy;
            set => PerformedBy = value;
        }
    }
}

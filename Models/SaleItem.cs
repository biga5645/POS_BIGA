using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📦 عنصر من عناصر البيع - Sale Line Item
    /// </summary>
    public class SaleItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>رقم البيع</summary>
        public int SaleId { get; set; }

        [ForeignKey("SaleId")]
        public virtual Sale Sale { get; set; }

        /// <summary>رقم المنتج</summary>
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }

        // ======================================
        // 📝 معلومات المنتج (Snapshot)
        // ======================================

        /// <summary>اسم المنتج (نسخة وقت البيع)</summary>
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; }

        /// <summary>الباركود (نسخة وقت البيع)</summary>
        [MaxLength(100)]
        public string ProductBarcode { get; set; }

        /// <summary>اسم الوحدة (نسخة وقت البيع)</summary>
        [MaxLength(50)]
        public string UnitName { get; set; }

        // ======================================
        // 💰 الكميات والأسعار
        // ======================================

        /// <summary>الكمية</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Required]
        public decimal Quantity { get; set; }

        /// <summary>سعر الوحدة</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>المجموع = Quantity × UnitPrice</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal LineTotal { get; set; }

        /// <summary>مبلغ الخصم على هذا السطر</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>نسبة الخصم (%)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        /// <summary>المجموع بعد الخصم</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal LineTotalAfterDiscount { get; set; }

        // ======================================
        // 🧮 Helper Methods
        // ======================================

        /// <summary>
        /// إعادة حساب السطر
        /// </summary>
        public void RecalculateLine()
        {
            // 1. حساب المجموع الأساسي
            LineTotal = System.Math.Round(UnitPrice * Quantity, 2);

            // 2. حساب الخصم
            decimal discountFromPercent = LineTotal * (DiscountPercent / 100m);
            DiscountAmount = System.Math.Max(DiscountAmount, discountFromPercent);

            // 3. المجموع بعد الخصم
            LineTotalAfterDiscount = LineTotal - DiscountAmount;

            // تقريب
            DiscountAmount = System.Math.Round(DiscountAmount, 2);
            LineTotalAfterDiscount = System.Math.Round(LineTotalAfterDiscount, 2);
        }

        // ======================================
        // 🔄 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق - استخدم ProductName</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use ProductName instead")]
        public string Name
        {
            get => ProductName;
            set => ProductName = value;
        }
    }
}

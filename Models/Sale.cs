using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 🛒 نموذج البيع - Sale Model
    /// يدعم البيع النقدي والكريدي مع تتبع كامل
    /// </summary>
    public class Sale
    {
        [Key]
        public int Id { get; set; }

        /// <summary>رقم الفاتورة الفريد</summary>
        [Required]
        [MaxLength(50)]
        public string SaleNumber { get; set; }

        /// <summary>الزبون (اختياري - null للبيع النقدي بدون زبون)</summary>
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        /// <summary>تاريخ ووقت البيع</summary>
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        // ======================================
        // 💰 المبالغ - Amounts
        // ======================================

        /// <summary>المجموع الجزئي (قبل الضريبة والخصم)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        /// <summary>مبلغ الضريبة</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        /// <summary>نسبة الضريبة المطبقة (%)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        /// <summary>مبلغ الخصم</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>نسبة الخصم (%)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        /// <summary>المجموع الكلي النهائي</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>المبلغ المدفوع</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        /// <summary>المبلغ المتبقي (للكريدي)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        // ======================================
        // 📝 معلومات إضافية
        // ======================================

        /// <summary>نوع الدفع: Cash, Credit, Mixed</summary>
        [Required]
        [MaxLength(50)]
        public string PaymentType { get; set; } = "Cash";

        /// <summary>حالة البيع: Completed, Pending, Cancelled</summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Completed";

        /// <summary>ملاحظات</summary>
        [MaxLength(500)]
        public string Notes { get; set; }

        /// <summary>الكاشير / البائع</summary>
        [MaxLength(100)]
        public string Cashier { get; set; }

        /// <summary>رقم الصندوق / Terminal</summary>
        [MaxLength(50)]
        public string RegisterId { get; set; }

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

        /// <summary>عناصر البيع</summary>
        public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

        /// <summary>المدفوعات المرتبطة</summary>
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        /// <summary>الديون المرتبطة</summary>
        public virtual ICollection<Credit> Credits { get; set; } = new List<Credit>();

        // ======================================
        // 🧮 Helper Methods
        // ======================================

        /// <summary>
        /// حساب المجاميع أوتوماتيكياً من عناصر البيع
        /// </summary>
        public void CalculateTotals()
        {
            // 1. حساب SubTotal من SaleItems
            SubTotal = Items?.Sum(i => i.LineTotalAfterDiscount) ?? 0m;

            // 2. حساب الخصم الإضافي
            decimal discountFromPercent = SubTotal * (DiscountPercent / 100m);
            DiscountAmount = Math.Max(DiscountAmount, discountFromPercent);

            // 3. المجموع بعد الخصم
            decimal subtotalAfterDiscount = SubTotal - DiscountAmount;

            // 4. حساب الضريبة
            TaxAmount = subtotalAfterDiscount * (TaxRate / 100m);

            // 5. المجموع النهائي
            TotalAmount = subtotalAfterDiscount + TaxAmount;

            // 6. المتبقي
            RemainingAmount = TotalAmount - PaidAmount;

            // تقريب
            SubTotal = Math.Round(SubTotal, 2);
            DiscountAmount = Math.Round(DiscountAmount, 2);
            TaxAmount = Math.Round(TaxAmount, 2);
            TotalAmount = Math.Round(TotalAmount, 2);
            RemainingAmount = Math.Round(RemainingAmount, 2);
        }

        /// <summary>
        /// هل البيع نقدي بالكامل؟
        /// </summary>
        [NotMapped]
        public bool IsCashSale => PaymentType == "Cash" && RemainingAmount == 0;

        /// <summary>
        /// هل البيع كريدي (جزئي أو كامل)؟
        /// </summary>
        [NotMapped]
        public bool IsCreditSale => PaymentType == "Credit" || PaymentType == "Mixed" || RemainingAmount > 0;

        /// <summary>
        /// هل البيع مكتمل الدفع؟
        /// </summary>
        [NotMapped]
        public bool IsFullyPaid => RemainingAmount <= 0;

        /// <summary>
        /// المبلغ المدفوع من الـ Payments
        /// </summary>
        [NotMapped]
        public decimal TotalPaymentsAmount => Payments?.Sum(p => p.Amount) ?? 0m;

        // ======================================
        // 🔄 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق - استخدم TotalAmount</summary>
        [NotMapped]
        [Obsolete("Use TotalAmount instead")]
        public decimal Total
        {
            get => TotalAmount;
            set => TotalAmount = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم TaxAmount</summary>
        [NotMapped]
        [Obsolete("Use TaxAmount instead")]
        public decimal Tax
        {
            get => TaxAmount;
            set => TaxAmount = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم DiscountAmount</summary>
        [NotMapped]
        [Obsolete("Use DiscountAmount instead")]
        public decimal Discount
        {
            get => DiscountAmount;
            set => DiscountAmount = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم PaidAmount</summary>
        [NotMapped]
        [Obsolete("Use PaidAmount instead")]
        public decimal AmountPaid
        {
            get => PaidAmount;
            set => PaidAmount = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم RemainingAmount</summary>
        [NotMapped]
        [Obsolete("Use RemainingAmount instead")]
        public decimal AmountDue => RemainingAmount;
    }
}

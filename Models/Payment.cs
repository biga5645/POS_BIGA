using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 💵 نموذج التسديد - Payment Model
    /// تسديد جزئي أو كامل لدين معين
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>رقم الدين المرتبط</summary>
        [Required]
        public int CreditId { get; set; }

        [ForeignKey("CreditId")]
        public virtual Credit Credit { get; set; }

        /// <summary>مبلغ الدفعة</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal Amount { get; set; }

        /// <summary>طريقة الدفع</summary>
        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>تاريخ الدفع</summary>
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        /// <summary>رقم مرجعي (رقم الشيك، رقم التحويل، إلخ)</summary>
        [MaxLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>ملاحظات</summary>
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>اسم من استلم الدفعة</summary>
        [MaxLength(100)]
        public string ReceivedBy { get; set; } = "System";

        /// <summary>تاريخ الإنشاء</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ======================================
        // 🔄 Backward Compatibility (للكود القديم المرتبط بـ Sale)
        // ======================================

        /// <summary>للحفاظ على التوافق - لم يعد مستخدماً (Payment الآن مرتبط بـ Credit)</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Payment is now linked to Credit, not Sale")]
        public int SaleId => Credit?.SaleId ?? 0;

        /// <summary>للحفاظ على التوافق - استخدم PaymentMethod</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use PaymentMethod property instead")]
        public PaymentMethod Method
        {
            get => PaymentMethod;
            set => PaymentMethod = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم ReferenceNumber</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use ReferenceNumber instead")]
        public string Reference
        {
            get => ReferenceNumber;
            set => ReferenceNumber = value;
        }

        /// <summary>للحفاظ على التوافق - استخدم PaymentDate</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use PaymentDate instead")]
        public DateTime PaidAt
        {
            get => PaymentDate;
            set => PaymentDate = value;
        }
    }

    /// <summary>
    /// طرق الدفع المتاحة
    /// </summary>
    public enum PaymentMethod
    {
        Cash = 0,       // نقدي
        Card = 1,       // بطاقة
        BankTransfer = 2, // تحويل بنكي
        Check = 3,      // شيك
        Mobile = 4,     // محفظة إلكترونية
        Voucher = 5,    // قسيمة
        Other = 99      // أخرى
    }
}

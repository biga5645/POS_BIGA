using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 💳 نموذج الدين - Credit Model
    /// تصميم احترافي للكريدي مع تتبع التسديدات
    /// </summary>
    public enum CreditStatus
    {
        Unpaid,         // غير مسدد
        PartiallyPaid,  // مسدد جزئياً
        Paid,           // مسدد كلياً
        Overdue,        // متأخر
        Cancelled       // ملغي
    }

    public class Credit : INotifyPropertyChanged
    {
        private int _id;
        [Key]
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        // ======================================
        // 🔗 العلاقات
        // ======================================

        private int _customerId;
        [Required]
        public int CustomerId
        {
            get => _customerId;
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        public virtual Customer Customer { get; set; }

        private int? _saleId;
        public int? SaleId
        {
            get => _saleId;
            set { _saleId = value; OnPropertyChanged(nameof(SaleId)); }
        }

        public virtual Sale Sale { get; set; }

        // ======================================
        // 💰 المبالغ
        // ======================================

        /// <summary>المبلغ الأصلي للدين</summary>
        private decimal _originalAmount;
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal OriginalAmount
        {
            get => _originalAmount;
            set { _originalAmount = value; OnPropertyChanged(nameof(OriginalAmount)); }
        }

        /// <summary>المبلغ المسدد حتى الآن</summary>
        private decimal _paidAmount;
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount
        {
            get => _paidAmount;
            set 
            { 
                _paidAmount = value; 
                OnPropertyChanged(nameof(PaidAmount)); 
                OnPropertyChanged(nameof(RemainingAmount));
                UpdateStatus();
            }
        }

        /// <summary>المبلغ المتبقي</summary>
        private decimal _remainingAmount;
        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal RemainingAmount
        {
            get => _remainingAmount;
            set { _remainingAmount = value; OnPropertyChanged(nameof(RemainingAmount)); }
        }

        // ======================================
        // 📝 معلومات إضافية
        // ======================================

        private CreditStatus _status = CreditStatus.Unpaid;
        [Required]
        public CreditStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); UpdateStatus(); }
        }

        private string _notes;
        [MaxLength(500)]
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        // ======================================
        // 📅 Audit Trail
        // ======================================

        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(nameof(CreatedAt)); }
        }

        private DateTime? _updatedAt;
        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(nameof(UpdatedAt)); }
        }

        private string _createdBy;
        [MaxLength(100)]
        public string CreatedBy
        {
            get => _createdBy;
            set { _createdBy = value; OnPropertyChanged(nameof(CreatedBy)); }
        }

        // ======================================
        // 🔗 Navigation Properties
        // ======================================

        /// <summary>المدفوعات (تسديدات هذا الدين)</summary>
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // ======================================
        // 🧮 Helper Methods
        // ======================================

        /// <summary>
        /// تحديث المبلغ المتبقي بناءً على التسديدات
        /// </summary>
        public void UpdateRemainingAmount()
        {
            PaidAmount = Payments?.Sum(p => p.Amount) ?? 0m;
            RemainingAmount = OriginalAmount - PaidAmount;
            RemainingAmount = Math.Max(0, RemainingAmount);
            UpdateStatus();
        }

        /// <summary>
        /// تحديث الحالة أوتوماتيكياً
        /// </summary>
        private void UpdateStatus()
        {
            if (RemainingAmount <= 0)
            {
                Status = CreditStatus.Paid;
            }
            else if (PaidAmount > 0)
            {
                Status = CreditStatus.PartiallyPaid;
            }
            else if (DueDate.HasValue && DueDate.Value < DateTime.Now)
            {
                Status = CreditStatus.Overdue;
            }
            else
            {
                Status = CreditStatus.Unpaid;
            }
        }

        /// <summary>
        /// هل الدين متأخر؟
        /// </summary>
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Now && RemainingAmount > 0;

        /// <summary>
        /// عدد الأيام المتأخرة
        /// </summary>
        [NotMapped]
        public int DaysOverdue => IsOverdue ? (int)(DateTime.Now - DueDate.Value).TotalDays : 0;

        /// <summary>
        /// نسبة التسديد
        /// </summary>
        [NotMapped]
        public decimal PaymentPercentage => OriginalAmount > 0 ? (PaidAmount / OriginalAmount) * 100 : 0;

        // ======================================
        // 🔄 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق مع الكود القديم - استخدم OriginalAmount</summary>
        [NotMapped]
        [Obsolete("Use OriginalAmount instead")]
        public decimal Amount
        {
            get => OriginalAmount;
            set => OriginalAmount = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

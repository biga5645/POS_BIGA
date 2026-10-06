using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 👤 نموذج الزبون - Customer Model
    /// تصميم احترافي يدعم الكريدي والتتبع الكامل
    /// </summary>
    public class Customer : INotifyPropertyChanged
    {
        private int _id;
        [Key]
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        private string _fullName = string.Empty;
        [Required]
        [MaxLength(200)]
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }

        private string _phone;
        [MaxLength(20)]
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        private string _email;
        [EmailAddress]
        [MaxLength(200)]
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        private string _address;
        [MaxLength(500)]
        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
        }

        private string _notes;
        [MaxLength(2000)]
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        // 💰 Credit Limit - سقف الكريدي المسموح
        private decimal _creditLimit = 0m;
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit
        {
            get => _creditLimit;
            set { _creditLimit = value; OnPropertyChanged(nameof(CreditLimit)); OnPropertyChanged(nameof(AvailableCredit)); }
        }

        // ✅ حالة النشاط - Soft Delete
        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        // 📅 Audit Trail
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

        private string _createdBy = "System"; // ✅ قيمة افتراضية
        [MaxLength(100)]
        public string CreatedBy
        {
            get => _createdBy;
            set { _createdBy = value; OnPropertyChanged(nameof(CreatedBy)); }
        }

        // ======================================
        // 🔗 Navigation Properties
        // ======================================

        /// <summary>مبيعات الزبون</summary>
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

        /// <summary>ديون الزبون</summary>
        public virtual ICollection<Credit> Credits { get; set; } = new List<Credit>();

        // ======================================
        // 🧮 Computed Properties (غير مخزنة في DB)
        // ======================================

        /// <summary>
        /// إجمالي الديون الحالية (محسوب ديناميكياً)
        /// </summary>
        [NotMapped]
        public decimal TotalDebt => Credits?.Where(c => c.RemainingAmount > 0).Sum(c => c.RemainingAmount) ?? 0m;

        /// <summary>
        /// الكريدي المتاح = CreditLimit - TotalDebt
        /// </summary>
        [NotMapped]
        public decimal AvailableCredit => Math.Max(0, CreditLimit - TotalDebt);

        /// <summary>
        /// هل يمكن البيع بالكريدي؟
        /// </summary>
        [NotMapped]
        public bool CanBuyOnCredit => IsActive && TotalDebt < CreditLimit;

        /// <summary>
        /// تاريخ آخر دين
        /// </summary>
        [NotMapped]
        public DateTime? LastCreditDate => Credits?.OrderByDescending(c => c.CreatedAt).FirstOrDefault()?.CreatedAt;

        /// <summary>
        /// عدد الديون المفتوحة
        /// </summary>
        [NotMapped]
        public int OpenCreditsCount => Credits?.Count(c => c.RemainingAmount > 0) ?? 0;

        // ======================================
        // 🔄 Backward Compatibility
        // ======================================

        /// <summary>للحفاظ على التوافق - استخدم TotalDebt (computed dynamically)</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use TotalDebt property instead (computed dynamically)")]
        public decimal OutstandingDebt
        {
            get => TotalDebt;
            set { /* Ignored - TotalDebt is computed */ }
        }

        /// <summary>للحفاظ على التوافق - استخدم TotalDebt</summary>
        [NotMapped]
        [System.ObsoleteAttribute("Use TotalDebt property instead")]
        public decimal CurrentDebt
        {
            get => TotalDebt;
            set { /* Ignored - TotalDebt is computed */ }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

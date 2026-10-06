using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📏 نموذج الوحدة - Unit of Measure Model
    /// </summary>
    public class Unit : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        private string _name = string.Empty;
        /// <summary>اسم الوحدة (مثل: قطعة، لتر، كيلو)</summary>
        [Required]
        [MaxLength(100)]
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private string _abbreviation = string.Empty;
        /// <summary>اختصار أو رمز الوحدة (مثل: pcs, l, kg)</summary>
        [MaxLength(20)]
        public string Abbreviation
        {
            get => _abbreviation;
            set { if (_abbreviation != value) { _abbreviation = value; OnPropertyChanged(nameof(Abbreviation)); } }
        }

        private bool _isActive = true;
        /// <summary>حالة النشاط</summary>
        public bool IsActive
        {
            get => _isActive;
            set { if (_isActive != value) { _isActive = value; OnPropertyChanged(nameof(IsActive)); } }
        }

        /// <summary>تاريخ الإنشاء</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✨ خصائص البيع بالتجزئة

        private bool _isDivisible;
        /// <summary>هل الوحدة قابلة للتجزئة (مثل: كرتونة = 12 قطعة)</summary>
        public bool IsDivisible
        {
            get => _isDivisible;
            set { if (_isDivisible != value) { _isDivisible = value; OnPropertyChanged(nameof(IsDivisible)); } }
        }

        private string? _subUnitName;
        /// <summary>اسم الوحدة الفرعية (مثل: قطعة، علبة، حبة)</summary>
        [MaxLength(100)]
        public string? SubUnitName
        {
            get => _subUnitName;
            set { if (_subUnitName != value) { _subUnitName = value; OnPropertyChanged(nameof(SubUnitName)); } }
        }

        private decimal _subUnitQuantity = 1;
        /// <summary>عدد الوحدات الفرعية في الوحدة الرئيسية (مثل: 12 قطعة في كرتونة)</summary>
        public decimal SubUnitQuantity
        {
            get => _subUnitQuantity;
            set { if (_subUnitQuantity != value) { _subUnitQuantity = value; OnPropertyChanged(nameof(SubUnitQuantity)); } }
        }

        // ✨ خاصية السماح بإدخال الثمن المخصص

        private bool _allowCustomPrice;
        /// <summary>السماح بإدخال الثمن المخصص في السلة (للوزن، المتر، اللتر، إلخ)</summary>
        public bool AllowCustomPrice
        {
            get => _allowCustomPrice;
            set 
            { 
                if (_allowCustomPrice != value) 
                { 
                    _allowCustomPrice = value; 
                    OnPropertyChanged(nameof(AllowCustomPrice)); 
                } 
            }
        }

        public override string ToString() => Name;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
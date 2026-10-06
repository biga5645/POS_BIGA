using System.ComponentModel;

namespace POS_BIGA.Models
{
    public class CartItem : INotifyPropertyChanged
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }

        // ✅ إضافة أسعار البيع المتعددة
        public decimal? WholesalePrice { get; set; }
        public decimal? InstallmentPrice { get; set; }

        // ✅ نوع سعر البيع المستخدم
        private PriceType _selectedPriceType = PriceType.Retail;
        public PriceType SelectedPriceType
        {
            get => _selectedPriceType;
            set
            {
                if (_selectedPriceType == value) return;
                _selectedPriceType = value;
                OnPropertyChanged(nameof(SelectedPriceType));
                OnPropertyChanged(nameof(ActiveUnitPrice));
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        /// <summary>السعر الفعلي المستخدم بناءً على نوع البيع</summary>
        public decimal ActiveUnitPrice => SelectedPriceType switch
        {
            PriceType.Wholesale   => WholesalePrice   ?? UnitPrice,
            PriceType.Installment => InstallmentPrice ?? UnitPrice,
            _                     => UnitPrice
        };

        // ✅ إضافة نوع الوحدة
        public string Unit { get; set; }

        // ✅ هل الوحدة تسمح بإدخال الثمن المخصص؟
        public bool AllowCustomPrice { get; set; }

        // ✅ تحديد نوع الإدخال (للتوافق مع الكود القديم)
        public bool IsPiece => !AllowCustomPrice;

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                // prevent negative quantities
                var newVal = value < 0m ? 0m : value;

                // ✅ للقطع: عدد صحيح فقط
                if (IsPiece && newVal != System.Math.Floor(newVal))
                {
                    newVal = System.Math.Floor(newVal);
                }

                if (_quantity == newVal) return;
                _quantity = newVal;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(TotalPrice));

                // مسح الثمن المخصص عند تغيير الكمية
                if (_customPrice > 0)
                {
                    _customPrice = 0;
                    OnPropertyChanged(nameof(CustomPrice));
                }
            }
        }

        private decimal _customPrice;
        /// <summary>
        /// الثمن المدخل يدوياً (للوزن والمتر فقط)
        /// </summary>
        public decimal CustomPrice
        {
            get => _customPrice;
            set
            {
                // السماح فقط للوزن والمتر
                if (IsPiece)
                {
                    _customPrice = 0;
                    return;
                }

                var newVal = value < 0m ? 0m : value;
                if (_customPrice == newVal) return;
                _customPrice = newVal;

                // حساب الكمية من الثمن
                if (_customPrice > 0 && UnitPrice > 0)
                {
                    _quantity = System.Math.Round(_customPrice / UnitPrice, 3);
                    OnPropertyChanged(nameof(Quantity));
                }

                OnPropertyChanged(nameof(CustomPrice));
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public decimal TotalPrice
        {
            get
            {
                // إذا تم إدخال ثمن مخصص، استخدمه
                if (_customPrice > 0)
                    return System.Math.Round(_customPrice, 2);

                // استخدام السعر الفعلي حسب نوع البيع
                return System.Math.Round(ActiveUnitPrice * Quantity, 2);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

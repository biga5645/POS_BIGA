using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_BIGA.Models
{
    /// <summary>
    /// 📂 نموذج الفئة - Category Model
    /// تصنيف هرمي للمنتجات
    /// </summary>
    public class Category : INotifyPropertyChanged
    {
        private int _id;
        [Key]
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        private string _name = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _description = string.Empty;
        [MaxLength(500)]
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private string _imageUrl = string.Empty;
        [MaxLength(500)]
        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(nameof(ImageUrl)); }
        }

        // ======================================
        // 🌲 التصنيف الهرمي
        // ======================================

        /// <summary>الفئة الأب (اختياري)</summary>
        private int? _parentCategoryId;
        public int? ParentCategoryId
        {
            get => _parentCategoryId;
            set { _parentCategoryId = value; OnPropertyChanged(nameof(ParentCategoryId)); }
        }

        [ForeignKey("ParentCategoryId")]
        public virtual Category ParentCategory { get; set; }

        /// <summary>الفئات الفرعية</summary>
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // ======================================
        // ✅ حالة النشاط
        // ======================================

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
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

        // ======================================
        // 🔗 Navigation Properties
        // ======================================

        /// <summary>المنتجات في هذه الفئة</summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public override string ToString() => Name;
    }
}

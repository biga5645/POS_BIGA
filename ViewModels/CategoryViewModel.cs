using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Services;
using POS_BIGA.Models;
using POS_BIGA.Infrastructure.Helpers;

namespace POS_BIGA.ViewModels
{
    public class CategoryViewModel : INotifyPropertyChanged
    {
        private readonly ICategoryService _service;
        private readonly IAppStateService _appState;

        // المسار المحلي المؤقت للصورة التي اختارها المستخدم (قبل الحفظ)
        private string? _pendingLocalImagePath;

        public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                _pendingLocalImagePath = null; // إلغاء أي صورة مؤقتة عند تغيير الفئة
                if (_selectedCategory != null)
                {
                    NewCategoryName = _selectedCategory.Name;
                    NewCategoryImage = _selectedCategory.ImageUrl;
                    NewCategoryDescription = _selectedCategory.Description;
                    IsCategoryActive = _selectedCategory.IsActive;
                }
                else
                {
                    NewCategoryName = string.Empty;
                    NewCategoryImage = string.Empty;
                    NewCategoryDescription = string.Empty;
                    IsCategoryActive = true;
                }
                OnPropertyChanged(nameof(SelectedCategory));
                ((RelayCommand)EditCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DeleteCommand).NotifyCanExecuteChanged();
            }
        }

        private string _newCategoryName;
        public string NewCategoryName
        {
            get => _newCategoryName;
            set { _newCategoryName = value; OnPropertyChanged(nameof(NewCategoryName)); ((RelayCommand)AddCommand).NotifyCanExecuteChanged(); }
        }

        private string _newCategoryDescription = string.Empty;
        public string NewCategoryDescription
        {
            get => _newCategoryDescription;
            set { _newCategoryDescription = value; OnPropertyChanged(nameof(NewCategoryDescription)); }
        }

        private bool _isCategoryActive = true;
        public bool IsCategoryActive
        {
            get => _isCategoryActive;
            set { _isCategoryActive = value; OnPropertyChanged(nameof(IsCategoryActive)); }
        }

        private string _newCategoryImage;
        public string NewCategoryImage
        {
            get => _newCategoryImage;
            set { _newCategoryImage = value; OnPropertyChanged(nameof(NewCategoryImage)); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand BrowseImageCommand { get; }
        public ICommand ApplyImageToSelectedCommand { get; }
        public ICommand SelectForEditCommand { get; }
        public ICommand DeleteItemCommand { get; }

        public CategoryViewModel(ICategoryService service, IAppStateService appState)
        {
            _service = service;
            _appState = appState;

            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            AddCommand = new RelayCommand(async _ => await AddAsync(), _ => !string.IsNullOrWhiteSpace(NewCategoryName));
            EditCommand = new RelayCommand(async _ => await EditAsync(), _ => SelectedCategory != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedCategory != null);
            BrowseImageCommand = new RelayCommand(_ => BrowseImage());
            ApplyImageToSelectedCommand = new RelayCommand(async _ => await ApplyImageToSelectedAsync(), _ => SelectedCategory != null);
            SelectForEditCommand = new RelayCommand(SelectForEdit);
            DeleteItemCommand = new RelayCommand(async param => await DeleteItemAsync(param));

            // Subscribe to app state events
            _appState.CategoriesChanged += AppState_CategoriesChanged;

            _ = LoadAsync();
        }

        private void AppState_CategoriesChanged(object sender, EventArgs e)
        {
            // reload on UI thread
            _ = Application.Current.Dispatcher.InvokeAsync(async () => await LoadAsync());
        }

        public async Task LoadAsync()
        {
            try
            {
                Categories.Clear();
                var list = await _service.GetAllAsync();
                foreach (var c in list)
                    Categories.Add(c);
                SelectedCategory = Categories.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task AddAsync()
        {
            try
            {
                var name = (NewCategoryName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("الرجاء إدخال اسم الفئة.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (await _service.ExistsByNameAsync(name))
                {
                    MessageBox.Show("هذه الفئة موجودة بالفعل.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ نسخ الصورة المؤقتة إلى المجلد المركزي عند الحفظ
                var imageUrl = ResolveImageUrl(name);

                var cat = new Category
                {
                    Name = name,
                    Description = NewCategoryDescription ?? string.Empty,
                    ImageUrl = imageUrl,
                    IsActive = IsCategoryActive
                };
                await _service.AddAsync(cat);

                _pendingLocalImagePath = null;
                NewCategoryName = string.Empty;
                NewCategoryDescription = string.Empty;
                NewCategoryImage = string.Empty;
                IsCategoryActive = true;

                MessageBox.Show($"✅ تمت إضافة الفئة: {name}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة الفئة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task EditAsync()
        {
            if (SelectedCategory == null) return;
            try
            {
                var name = (NewCategoryName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("الرجاء إدخال اسم الفئة.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.Equals(SelectedCategory.Name, name, StringComparison.OrdinalIgnoreCase) && await _service.ExistsByNameAsync(name))
                {
                    MessageBox.Show("هذه الفئة موجودة بالفعل.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ✅ نسخ الصورة المؤقتة إلى المجلد المركزي عند الحفظ
                var imageUrl = ResolveImageUrl(name, SelectedCategory.ImageUrl);

                SelectedCategory.Name = name;
                SelectedCategory.Description = NewCategoryDescription ?? string.Empty;
                SelectedCategory.ImageUrl = imageUrl;
                SelectedCategory.IsActive = IsCategoryActive;

                await _service.UpdateAsync(SelectedCategory);

                _pendingLocalImagePath = null;
                NewCategoryImage = imageUrl;

                MessageBox.Show($"✅ تم تحديث الفئة", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث الفئة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteAsync()
        {
            if (SelectedCategory == null) return;
            try
            {
                if (MessageBox.Show($"هل أنت متأكد من حذف {SelectedCategory.Name}?", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _service.DeleteAsync(SelectedCategory.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حذف الفئة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحويل الصورة المؤقتة (مسار محلي) إلى مسار نسبي /Images/Categories/اسم.jpg
        /// نفس منطق صور المنتجات في ProductEditorWindow
        /// </summary>
        private string ResolveImageUrl(string name, string? existingImageUrl = null)
        {
            // إذا كان المستخدم اختار صورة محلية → انسخها إلى المجلد المركزي
            if (!string.IsNullOrWhiteSpace(_pendingLocalImagePath))
            {
                try
                {
                    // يُرجع /Images/Categories/اسم.jpg مباشرة
                    return ImageStorageHelper.SaveToCentralFolder(_pendingLocalImagePath, "Categories", name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حفظ صورة الفئة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // إذا أدخل المستخدم رابطاً أو مساراً يدوياً في TextBox
            var currentImage = NewCategoryImage ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentImage) && !IsLocalAbsolutePath(currentImage))
                return currentImage;

            // الاحتفاظ بالصورة القديمة إن وُجدت
            if (!string.IsNullOrWhiteSpace(existingImageUrl))
                return existingImageUrl;

            // ✅ مسار افتراضي تلقائي /Images/Categories/اسم.jpg
            return ImageStorageHelper.BuildDefaultImagePath(name, "Categories");
        }

        private static bool IsLocalAbsolutePath(string s)
            => Path.IsPathRooted(s) && !s.StartsWith("/") && !s.StartsWith("\\\\");

        /// <summary>
        /// استعراض صورة من النظام - معاينة فورية دون نسخ (نفس منطق صور المنتجات)
        /// </summary>
        private void BrowseImage()
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "اختيار صورة للفئة",
                    Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
                };

                if (dlg.ShowDialog() == true)
                {
                    // ✅ حفظ المسار المؤقت للنسخ لاحقاً عند الضغط على إضافة/حفظ
                    _pendingLocalImagePath = dlg.FileName;
                    // ✅ عرض الصورة فوراً للمعاينة من المسار المحلي
                    NewCategoryImage = dlg.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في اختيار الصورة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تطبيق الصورة على الفئة المختارة - MVVM Command
        /// </summary>
        private async Task ApplyImageToSelectedAsync()
        {
            if (SelectedCategory == null) return;

            try
            {
                var imageUrl = ResolveImageUrl(SelectedCategory.Name, SelectedCategory.ImageUrl);
                SelectedCategory.ImageUrl = imageUrl;
                await _service.UpdateAsync(SelectedCategory);

                _pendingLocalImagePath = null;
                NewCategoryImage = imageUrl;

                await LoadAsync();
                ((RelayCommand)EditCommand).NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث صورة الفئة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تحديد فئة للتعديل من DataGrid
        /// </summary>
        private void SelectForEdit(object parameter)
        {
            if (parameter is Category category)
            {
                SelectedCategory = category;
                NewCategoryName = category.Name;
                NewCategoryDescription = category.Description ?? string.Empty;
                IsCategoryActive = category.IsActive;
                NewCategoryImage = category.ImageUrl ?? string.Empty;

                System.Diagnostics.Debug.WriteLine($"📝 تم تحميل بيانات الفئة للتعديل: {category.Name}");
            }
        }

        /// <summary>
        /// حذف فئة من DataGrid مباشرة
        /// </summary>
        private async Task DeleteItemAsync(object parameter)
        {
            if (parameter is not Category category) return;

            try
            {
                if (MessageBox.Show($"هل أنت متأكد من حذف  {category.Name}?", "تأكيد",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await _service.DeleteAsync(category.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حذف الفئة : {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

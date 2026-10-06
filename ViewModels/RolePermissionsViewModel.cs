using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;

namespace POS_BIGA.ViewModels
{
    // ======================================
    // نموذج صلاحية واحدة مع تفعيلها لدور معين
    // ======================================
    public class PermissionItem : INotifyPropertyChanged
    {
        public Permission Permission { get; init; }
        public string     Label      { get; init; } = string.Empty;
        public string     Category   { get; init; } = string.Empty;
        public string     Icon       { get; init; } = string.Empty;

        private bool _isGranted;
        public bool IsGranted
        {
            get => _isGranted;
            set { _isGranted = value; OnPropertyChanged(nameof(IsGranted)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ======================================
    // مجموعة صلاحيات دور واحد
    // ======================================
    public class RolePermissionGroup : INotifyPropertyChanged
    {
        public UserRole Role        { get; init; }
        public string   RoleLabel   { get; init; } = string.Empty;
        public string   RoleIcon    { get; init; } = string.Empty;
        public string   RoleColor   { get; init; } = "#607D8B";
        public ObservableCollection<PermissionItem> Permissions { get; } = new();

        /// <summary>نفس القائمة مجمّعة بالفئة - للعرض في ItemsControl</summary>
        public ListCollectionView GroupedPermissions
        {
            get
            {
                var view = new ListCollectionView(Permissions);
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PermissionItem.Category)));
                return view;
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ======================================
    // ViewModel الرئيسي لإدارة الصلاحيات
    // ======================================
    public class RolePermissionsViewModel : INotifyPropertyChanged
    {
        private readonly IAppSettingService _settingService;
        private const string SettingKey = "RolePermissions";

        // تعريف جميع الصلاحيات مع أسمائها ومجموعاتها
        private static readonly List<(Permission P, string Label, string Category, string Icon)> AllPermissions = new()
        {
            // 🛒 المبيعات
            (Permission.CanSell,               "إجراء عمليات البيع",         "🛒 المبيعات",   "CashRegister"),
            (Permission.CanApplyDiscount,      "تطبيق الخصومات",              "🛒 المبيعات",   "PercentOutline"),
            (Permission.CanEditCartPrice,      "تعديل سعر المنتج في السلة",   "🛒 المبيعات",   "TagEdit"),
            (Permission.CanSuspendCart,        "تعليق السلة واسترجاعها",      "🛒 المبيعات",   "CartArrowDown"),
            (Permission.CanViewSalesHistory,   "عرض سجل المبيعات",            "🛒 المبيعات",   "History"),

            // 📦 المنتجات
            (Permission.CanViewProducts,       "عرض قائمة المنتجات",          "📦 المنتجات",   "PackageVariant"),
            (Permission.CanAddProduct,         "إضافة منتج جديد",             "📦 المنتجات",   "PackageVariantPlus"),
            (Permission.CanEditProduct,        "تعديل بيانات المنتج",         "📦 المنتجات",   "PackageVariantMinus"),
            (Permission.CanDeleteProduct,      "حذف المنتج",                  "📦 المنتجات",   "PackageVariantRemove"),
            (Permission.CanImportExportProducts,"استيراد/تصدير المنتجات",     "📦 المنتجات",   "FileExcel"),

            // 📊 التقارير
            (Permission.CanViewReports,        "عرض التقارير",                "📊 التقارير",   "ChartBar"),
            (Permission.CanExportReports,      "تصدير التقارير",              "📊 التقارير",   "FileExportOutline"),

            // 👥 العملاء
            (Permission.CanViewCustomers,      "عرض قائمة العملاء",           "👥 العملاء",    "AccountGroup"),
            (Permission.CanManageCredits,      "إدارة الديون والائتمان",      "👥 العملاء",    "CreditCard"),

            // 📋 المخزون
            (Permission.CanViewInventory,      "عرض المخزون",                 "📋 المخزون",   "Warehouse"),
            (Permission.CanManageInventory,    "إدارة المخزون",               "📋 المخزون",   "WarehouseEdit"),
            (Permission.CanManageCategories,   "إدارة الفئات",                "📋 المخزون",   "TagMultiple"),
            (Permission.CanManageUnits,        "إدارة وحدات القياس",          "📋 المخزون",   "Ruler"),

            // ⚙️ الإدارة
            (Permission.CanAccessSettings,     "الوصول إلى الإعدادات",        "⚙️ الإدارة",   "Cog"),
            (Permission.CanManageUsers,        "إدارة المستخدمين",            "⚙️ الإدارة",   "AccountCog"),
        };

        public ObservableCollection<RolePermissionGroup> Roles { get; } = new();

        private RolePermissionGroup? _selectedRole;
        public RolePermissionGroup? SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(nameof(SelectedRole)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        private bool _hasStatus;
        public bool HasStatus
        {
            get => _hasStatus;
            set { _hasStatus = value; OnPropertyChanged(nameof(HasStatus)); }
        }

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(nameof(IsError)); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); HasStatus = !string.IsNullOrEmpty(value); }
        }

        public ICommand SelectRoleCommand { get; }
        public ICommand SaveCommand       { get; }
        public ICommand ResetCommand      { get; }

        public RolePermissionsViewModel(IAppSettingService settingService)
        {
            _settingService = settingService;

            SelectRoleCommand = new RelayCommand(r => SelectRole(r as RolePermissionGroup));
            SaveCommand       = new RelayCommand(async _ => await SaveAsync(), _ => !IsLoading);
            ResetCommand      = new RelayCommand(async _ => await ResetToDefaultAsync(), _ => !IsLoading);

            _ = LoadAsync();
        }

        // ======================================
        // تحميل الصلاحيات المحفوظة
        // ======================================
        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                // تهيئة المجموعات
                BuildRoleGroups();

                // تحميل الصلاحيات المحفوظة من AppSettings
                var saved = await _settingService.GetByKeyAsync(SettingKey);
                if (saved != null && !string.IsNullOrWhiteSpace(saved.SettingValue))
                {
                    ApplySavedPermissions(saved.SettingValue);
                }

                // تحديد الدور الأول تلقائياً
                SelectRole(Roles.FirstOrDefault(r => r.Role == UserRole.Cashier));
            }
            finally
            {
                IsLoading = false;
            }
        }

        // بناء مجموعات الأدوار مع صلاحياتها الافتراضية
        private void BuildRoleGroups()
        {
            Roles.Clear();

            var defaultPerms = GetDefaultPermissions();

            var roleDefs = new[]
            {
                (UserRole.Cashier,   "الكاشير",       "CashRegister",  "#1976D2"),
                (UserRole.Inventory, "مسؤول المخزن",  "Warehouse",     "#FF9800"),
                (UserRole.Viewer,    "مشاهد",          "Eye",           "#4CAF50"),
            };

            foreach (var (role, label, icon, color) in roleDefs)
            {
                var group = new RolePermissionGroup
                {
                    Role       = role,
                    RoleLabel  = label,
                    RoleIcon   = icon,
                    RoleColor  = color,
                };

                var grantedForRole = defaultPerms.TryGetValue(role, out var set) ? set : new HashSet<Permission>();

                foreach (var (p, lbl, cat, ico) in AllPermissions)
                {
                    group.Permissions.Add(new PermissionItem
                    {
                        Permission = p,
                        Label      = lbl,
                        Category   = cat,
                        Icon       = ico,
                        IsGranted  = grantedForRole.Contains(p),
                    });
                }

                Roles.Add(group);
            }
        }

        // الصلاحيات الافتراضية لكل دور
        private static Dictionary<UserRole, HashSet<Permission>> GetDefaultPermissions() => new()
        {
            [UserRole.Cashier] = new HashSet<Permission>
            {
                Permission.CanSell, Permission.CanApplyDiscount, Permission.CanEditCartPrice,
                Permission.CanSuspendCart, Permission.CanViewProducts,
                Permission.CanViewCustomers, Permission.CanManageCredits, Permission.CanViewSalesHistory,
            },
            [UserRole.Inventory] = new HashSet<Permission>
            {
                Permission.CanViewProducts, Permission.CanAddProduct, Permission.CanEditProduct,
                Permission.CanDeleteProduct, Permission.CanImportExportProducts,
                Permission.CanViewInventory, Permission.CanManageInventory,
                Permission.CanManageCategories, Permission.CanManageUnits,
            },
            [UserRole.Viewer] = new HashSet<Permission>
            {
                Permission.CanViewProducts, Permission.CanViewReports,
                Permission.CanViewCustomers, Permission.CanViewInventory, Permission.CanViewSalesHistory,
            },
        };

        // تطبيق الصلاحيات المحفوظة من JSON
        private void ApplySavedPermissions(string json)
        {
            try
            {
                var saved = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, List<string>>>(json);
                if (saved == null) return;

                foreach (var group in Roles)
                {
                    var key = group.Role.ToString();
                    if (!saved.TryGetValue(key, out var perms)) continue;
                    var permSet = perms.Select(p =>
                        Enum.TryParse<Permission>(p, out var val) ? (Permission?)val : null)
                        .Where(p => p.HasValue).Select(p => p!.Value).ToHashSet();

                    foreach (var item in group.Permissions)
                        item.IsGranted = permSet.Contains(item.Permission);
                }
            }
            catch { /* تجاهل أخطاء التحليل، نستخدم الافتراضي */ }
        }

        private void SelectRole(RolePermissionGroup? group)
        {
            foreach (var r in Roles) r.IsSelected = false;
            _selectedRole = group;
            if (group != null) group.IsSelected = true;
            OnPropertyChanged(nameof(SelectedRole));
        }

        // ======================================
        // حفظ الصلاحيات في AppSettings + تطبيقها فوراً
        // ======================================
        private async Task SaveAsync()
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            try
            {
                // بناء قاموس الصلاحيات
                var dict = new Dictionary<string, List<string>>();
                foreach (var group in Roles)
                {
                    dict[group.Role.ToString()] = group.Permissions
                        .Where(p => p.IsGranted)
                        .Select(p => p.Permission.ToString())
                        .ToList();
                }

                var json = System.Text.Json.JsonSerializer.Serialize(dict);

                // حفظ في قاعدة البيانات
                await _settingService.SetValueAsync(SettingKey, json, " صلاحيات الأدوار المخصصة");

                // ✅ تطبيق فوري على جلسة العمل الحالية
                var permMap = dict.ToDictionary(
                    kv => Enum.Parse<UserRole>(kv.Key),
                    kv => kv.Value.Select(p => Enum.Parse<Permission>(p)).ToHashSet());

                SessionService.ApplyCustomPermissions(permMap);

                _isError = false;
                StatusMessage = " ✅ تم حفظ الصلاحيات وتطبيقها فوراً.";
            }
            catch (Exception ex)
            {
                _isError = true;
                StatusMessage = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // إعادة تعيين الصلاحيات للافتراضي
        private async Task ResetToDefaultAsync()
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            try
            {
                var defaults = GetDefaultPermissions();
                foreach (var group in Roles)
                {
                    var defSet = defaults.TryGetValue(group.Role, out var s) ? s : new HashSet<Permission>();
                    foreach (var item in group.Permissions)
                        item.IsGranted = defSet.Contains(item.Permission);
                }

                await _settingService.SetValueAsync(SettingKey, string.Empty, "صلاحيات الأدوار المخصصة ");
                SessionService.ResetToDefaultPermissions();

                _isError = false;
                StatusMessage = " ✅ تم إعادة التعيين للصلاحيات الافتراضية.";
            }
            catch (Exception ex)
            {
                _isError = true;
                StatusMessage = $"❌ خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

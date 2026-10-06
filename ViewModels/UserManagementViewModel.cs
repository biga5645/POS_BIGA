using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;

namespace POS_BIGA.ViewModels
{
    public class UserManagementViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;

        public UserManagementViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            Roles = new ObservableCollection<UserRoleItem>
            {
                new UserRoleItem { Role = UserRole.Admin,     Label = " مدير النظام"    },
                new UserRoleItem { Role = UserRole.Cashier,   Label = "كاشير "          },
                new UserRoleItem { Role = UserRole.Inventory, Label = "مسؤول المخزن "  },
                new UserRoleItem { Role = UserRole.Viewer,    Label = "مشاهد "          },
            };

            LoadCommand         = new RelayCommand(async _ => await LoadAsync());
            SaveCommand         = new RelayCommand(async _ => await SaveAsync(), _ => !IsLoading);
            DeleteCommand       = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedUser != null && !IsLoading);
            NewUserCommand      = new RelayCommand(_ => PrepareNewUser());
            SelectUserCommand   = new RelayCommand(u => SelectUser(u as AppUser));
            ToggleActiveCommand = new RelayCommand(async _ => await ToggleActiveAsync(), _ => SelectedUser != null);

            _ = LoadAsync();
        }

        // ======================================
        // 📋 القوائم والخصائص
        // ======================================

        public ObservableCollection<AppUser> Users { get; } = new();
        public ObservableCollection<UserRoleItem> Roles { get; }

        private AppUser? _selectedUser;
        public AppUser? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsNewUser));
                CommandManager.InvalidateRequerySuggested();
                if (value != null) LoadFormFromUser(value);
            }
        }

        // ======================================
        // 📝 حقول النموذج
        // ======================================

        private string _formUsername = string.Empty;
        public string FormUsername
        {
            get => _formUsername;
            set { _formUsername = value; OnPropertyChanged(nameof(FormUsername)); }
        }

        private string _formFullName = string.Empty;
        public string FormFullName
        {
            get => _formFullName;
            set { _formFullName = value; OnPropertyChanged(nameof(FormFullName)); }
        }

        private string _formPassword = string.Empty;
        public string FormPassword
        {
            get => _formPassword;
            set { _formPassword = value; OnPropertyChanged(nameof(FormPassword)); }
        }

        private string _formPin = string.Empty;
        public string FormPin
        {
            get => _formPin;
            set { _formPin = value; OnPropertyChanged(nameof(FormPin)); }
        }

        private UserRoleItem? _formSelectedRole;
        public UserRoleItem? FormSelectedRole
        {
            get => _formSelectedRole;
            set { _formSelectedRole = value; OnPropertyChanged(nameof(FormSelectedRole)); }
        }

        private bool _formIsActive = true;
        public bool FormIsActive
        {
            get => _formIsActive;
            set { _formIsActive = value; OnPropertyChanged(nameof(FormIsActive)); }
        }

        // ======================================
        // 💬 الحالة
        // ======================================

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); CommandManager.InvalidateRequerySuggested(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); OnPropertyChanged(nameof(HasStatus)); }
        }

        public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

        private bool _isError;
        public bool IsError
        {
            get => _isError;
            set { _isError = value; OnPropertyChanged(nameof(IsError)); }
        }

        public bool IsEditing => _selectedUser != null;
        public bool IsNewUser => _selectedUser == null;

        // ======================================
        // 📋 الأوامر
        // ======================================

        public ICommand LoadCommand         { get; }
        public ICommand SaveCommand         { get; }
        public ICommand DeleteCommand       { get; }
        public ICommand NewUserCommand      { get; }
        public ICommand SelectUserCommand   { get; }
        public ICommand ToggleActiveCommand { get; }

        // ======================================
        // ⚙️ العمليات
        // ======================================

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _userService.GetAllAsync();
                Users.Clear();
                foreach (var u in list) Users.Add(u);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ أثناء التحميل: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrepareNewUser()
        {
            _selectedUser = null;
            OnPropertyChanged(nameof(SelectedUser));
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(IsNewUser));
            FormUsername     = string.Empty;
            FormFullName     = string.Empty;
            FormPassword     = string.Empty;
            FormPin          = string.Empty;
            FormSelectedRole = Roles.FirstOrDefault(r => r.Role == UserRole.Cashier);
            FormIsActive     = true;
            StatusMessage    = string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }

        private void SelectUser(AppUser? user)
        {
            if (user == null) return;
            SelectedUser = user;
        }

        private void LoadFormFromUser(AppUser user)
        {
            FormUsername     = user.Username;
            FormFullName     = user.FullName;
            FormPassword     = string.Empty;
            FormPin          = string.Empty;
            FormSelectedRole = Roles.FirstOrDefault(r => r.Role == user.Role);
            FormIsActive     = user.IsActive;
            StatusMessage    = string.Empty;
        }

        private async Task SaveAsync()
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(FormUsername))
                    { ShowError("اسم المستخدم مطلوب."); return; }

                if (string.IsNullOrWhiteSpace(FormFullName))
                    { ShowError("الاسم الكامل مطلوب."); return; }

                var role = FormSelectedRole?.Role ?? UserRole.Cashier;

                if (_selectedUser == null)
                {
                    // مستخدم جديد
                    if (string.IsNullOrWhiteSpace(FormPassword))
                        { ShowError("كلمة المرور مطلوبة للمستخدم الجديد."); return; }

                    // التحقق المسبق من PIN
                    if (!string.IsNullOrWhiteSpace(FormPin))
                    {
                        var pinVal = FormPin.Trim();
                        if (pinVal.Length < 6 || pinVal.Length > 8 || !pinVal.All(char.IsDigit))
                        { ShowError("رمز PIN يجب أن يتكون من 6 إلى 8 أرقام فقط."); return; }
                    }

                    var newUser = new AppUser
                    {
                        Username = FormUsername.Trim(),
                        FullName = FormFullName.Trim(),
                        Role     = role,
                        IsActive = FormIsActive,
                    };

                    var (ok, err) = await _userService.AddAsync(newUser, FormPassword);
                    if (!ok) { ShowError(err); return; }

                    // ✅ جلب المستخدم من DB للحصول على Id الحقيقي
                    if (!string.IsNullOrWhiteSpace(FormPin))
                    {
                        var saved = await _userService.GetByUsernameAsync(newUser.Username);
                        if (saved == null) { ShowError("تم الإضافة لكن تعذّر جلب المستخدم لحفظ PIN."); return; }

                        var (pinOk, pinErr) = await _userService.ChangePinAsync(saved.Id, FormPin.Trim());
                        if (!pinOk) { ShowError($"تم إضافة المستخدم لكن فشل حفظ PIN: {pinErr}"); return; }
                    }

                    ShowSuccess("✅ تم إضافة المستخدم بنجاح.");
                }
                else
                {
                    // تعديل مستخدم موجود
                    _selectedUser.Username = FormUsername.Trim();
                    _selectedUser.FullName = FormFullName.Trim();
                    _selectedUser.Role     = role;
                    _selectedUser.IsActive = FormIsActive;

                    var (ok, err) = await _userService.UpdateAsync(_selectedUser);
                    if (!ok) { ShowError(err); return; }

                    // تغيير كلمة المرور إذا أُدخلت
                    if (!string.IsNullOrWhiteSpace(FormPassword))
                    {
                        var (pwOk, pwErr) = await _userService.ChangePasswordAsync(_selectedUser.Id, FormPassword);
                        if (!pwOk) { ShowError(pwErr); return; }
                    }

                    // ✅ تغيير PIN مع التحقق المسبق
                    if (!string.IsNullOrWhiteSpace(FormPin))
                    {
                        var pinVal = FormPin.Trim();
                        if (pinVal.Length < 6 || pinVal.Length > 8 || !pinVal.All(char.IsDigit))
                        { ShowError("رمز PIN يجب أن يتكون من 6 إلى 8 أرقام فقط."); return; }

                        var (pinOk, pinErr) = await _userService.ChangePinAsync(_selectedUser.Id, pinVal);
                        if (!pinOk) { ShowError(pinErr); return; }
                    }

                    ShowSuccess("✅ تم تحديث المستخدم بنجاح.");
                }

                await LoadAsync();
                PrepareNewUser();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteAsync()
        {
            if (_selectedUser == null) return;

            var confirm = MessageBox.Show(
                $"هل أنت متأكد من حذف المستخدم '{_selectedUser.FullName}'؟",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var (ok, err) = await _userService.DeleteAsync(_selectedUser.Id);
                if (!ok) { ShowError(err); return; }

                ShowSuccess("✅ تم حذف المستخدم بنجاح.");
                await LoadAsync();
                PrepareNewUser();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (_selectedUser == null) return;
            _selectedUser.IsActive = !_selectedUser.IsActive;
            await _userService.UpdateAsync(_selectedUser);
            await LoadAsync();
            ShowSuccess(_selectedUser.IsActive ? "✅ تم تفعيل المستخدم." : "✅ تم تعطيل المستخدم.");
        }

        private void ShowError(string msg) { IsError = true; StatusMessage = msg; }
        private void ShowSuccess(string msg) { IsError = false; StatusMessage = msg; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>عنصر مساعد لعرض الأدوار في ComboBox</summary>
    public class UserRoleItem
    {
        public UserRole Role  { get; set; }
        public string   Label { get; set; } = string.Empty;
    }
}

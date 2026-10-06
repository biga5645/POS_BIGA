using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using POS_BIGA.Commands;
using POS_BIGA.Models;
using POS_BIGA.Services;

namespace POS_BIGA.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;

        public LoginViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            LoginCommand     = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
            LoginPinCommand  = new RelayCommand(async _ => await LoginByPinAsync(), _ => !IsLoading && PinInput.Length >= 6);
            AppendPinCommand = new RelayCommand(digit => AppendPin(digit?.ToString() ?? string.Empty));
            // ⌫ مسح آخر رقم
            ClearPinCommand  = new RelayCommand(_ =>
            {
                if (PinInput.Length > 0)
                    PinInput = PinInput[..^1];
            });
            // 🗑 مسح كامل
            ClearAllPinCommand = new RelayCommand(_ => PinInput = string.Empty);
        }

        // ======================================
        // 🔤 خصائص الإدخال
        // ======================================

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        private string _pinInput = string.Empty;
        public string PinInput
        {
            get => _pinInput;
            set
            {
                if (value.Length > 8) return;
                _pinInput = value;
                OnPropertyChanged(nameof(PinInput));
                OnPropertyChanged(nameof(PinDisplay));
                CommandManager.InvalidateRequerySuggested();

                // ✅ تسجيل دخول تلقائي عند اكتمال 6 أرقام
                if (_pinInput.Length >= 6)
                    _ = LoginByPinAsync();
            }
        }

        /// <summary>عرض PIN كنقاط •</summary>
        public string PinDisplay => new string('•', _pinInput.Length);

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); CommandManager.InvalidateRequerySuggested(); }
        }

        // ======================================
        // 🎯 نتيجة تسجيل الدخول
        // ======================================

        public AppUser? LoggedInUser { get; private set; }
        public bool LoginSucceeded => LoggedInUser != null;

        // ======================================
        // 📋 الأوامر
        // ======================================

        public ICommand LoginCommand    { get; }
        public ICommand LoginPinCommand { get; }
        public ICommand AppendPinCommand { get; }
        public ICommand ClearPinCommand  { get; }
        public ICommand ClearAllPinCommand { get; }

        // ======================================
        // 🔐 تسجيل الدخول
        // ======================================

        private async Task LoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = " الرجاء إدخال اسم المستخدم.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = " الرجاء إدخال كلمة المرور.";
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _userService.LoginAsync(Username.Trim(), Password);
                if (user == null)
                {
                    ErrorMessage = " اسم المستخدم أو كلمة المرور غير صحيحة.";
                    return;
                }

                LoggedInUser = user;
                SessionService.Login(user);
                LoginSuccessful?.Invoke(user);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoginByPinAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(PinInput) || PinInput.Length < 4)
            {
                ErrorMessage = "الرجاء إدخال PIN صحيح (4 أرقام على الأقل).";
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _userService.LoginByPinAsync(PinInput);
                if (user == null)
                {
                    ErrorMessage = "رمز PIN غير صحيح.";
                    PinInput = string.Empty;
                    return;
                }

                LoggedInUser = user;
                SessionService.Login(user);
                LoginSuccessful?.Invoke(user);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"خطأ: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                PinInput = string.Empty;
            }
        }

        private void AppendPin(string digit)
        {
            if (PinInput.Length < 8 && !string.IsNullOrEmpty(digit))
                PinInput += digit;
        }

        // ======================================
        // 📢 Events
        // ======================================

        /// <summary>يُطلق عند نجاح تسجيل الدخول</summary>
        public event Action<AppUser>? LoginSuccessful;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

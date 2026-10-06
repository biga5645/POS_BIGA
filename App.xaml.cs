using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;
using POS_BIGA.Data.Repositories;
using POS_BIGA.Models;
using POS_BIGA.Services;
using POS_BIGA.Behaviors;
using POS_BIGA.Views.Windows;

namespace POS_BIGA
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // ✅ Singleton AppStateService shared across all ViewModels and Pages
        public static readonly IAppStateService SharedAppState = new AppStateService();
        private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "POS_BIGA", "logs.txt");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ تسجيل سلوك SelectAll لجميع TextBox في التطبيق — مرة واحدة فقط
            TextBoxSelectAllBehavior.Register();

            // Register in Application Resources for backward compatibility
            this.Resources["AppStateService"] = SharedAppState;

            Debug.WriteLine("✅ App: AppStateService singleton initialized");

            // Global exception handlers to avoid sudden crashes and collect diagnostic info
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // ✅ عرض نافذة تسجيل الدخول قبل النافذة الرئيسية
            ShowLoginAndStart();
        }

        private async void ShowLoginAndStart()
        {
            try
            {
                // ✅ منع إغلاق التطبيق تلقائياً عند إغلاق LoginWindow
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var userService     = new UserService(new UserRepository());
                var settingService  = new AppSettingService();

                // ✅ تحميل الصلاحيات المخصصة من قاعدة البيانات
                await LoadSavedPermissionsAsync(settingService);

                // ✅ تحميل API Base URL وتحديث ImageStorageHelper cache
                await LoadApiBaseUrlAsync(settingService);

                // ✅ ضمان صحة كلمة مرور Admin الافتراضية عند كل تشغيل
                await EnsureAdminPasswordAsync(userService);

                bool hasUsers = await userService.HasUsersAsync();

                if (!hasUsers)
                {
                    // أول تشغيل بدون مستخدمين: فتح النافذة الرئيسية مباشرة
                    var adminUser = await userService.GetByIdAsync(1);
                    if (adminUser != null) SessionService.Login(adminUser);

                    OpenMainWindow();
                }
                else
                {
                    // عرض نافذة تسجيل الدخول - هي المسؤولة عن فتح MainWindow عند النجاح
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء بدء التطبيق:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>فتح النافذة الرئيسية وتعيينها كـ MainWindow</summary>
        public static void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            Current.MainWindow = mainWindow;
            // ✅ إغلاق التطبيق عند إغلاق النافذة الرئيسية فقط
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }

        /// <summary>تحميل الصلاحيات المحفوظة من قاعدة البيانات وتطبيقها</summary>
        private async Task LoadSavedPermissionsAsync(AppSettingService settingService)
        {
            try
            {
                var saved = await settingService.GetByKeyAsync("RolePermissions");
                if (saved == null || string.IsNullOrWhiteSpace(saved.SettingValue)) return;

                var dict = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, List<string>>>(saved.SettingValue);
                if (dict == null) return;

                var permMap = dict.ToDictionary(
                    kv => Enum.Parse<POS_BIGA.Models.UserRole>(kv.Key),
                    kv => kv.Value
                        .Select(p => Enum.TryParse<POS_BIGA.Models.Permission>(p, out var val) ? (POS_BIGA.Models.Permission?)val : null)
                        .Where(p => p.HasValue).Select(p => p!.Value).ToHashSet());

                SessionService.ApplyCustomPermissions(permMap);
                Debug.WriteLine("✅ Loaded saved role permissions");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSavedPermissions error: {ex.Message}");
            }
        }

        /// <summary>تحميل API Base URL من قاعدة البيانات وتطبيقه على ImageStorageHelper</summary>
        private async Task LoadApiBaseUrlAsync(AppSettingService settingService)
        {
            try
            {
                var setting = await settingService.GetByKeyAsync("ApiBaseUrl");
                if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
                {
                    POS_BIGA.Infrastructure.Helpers.ImageStorageHelper.SetBaseUrl(setting.SettingValue);
                    Debug.WriteLine($"✅ API Base URL loaded: {setting.SettingValue}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadApiBaseUrl error: {ex.Message}");
            }
        }

        /// <summary>
        /// يتأكد أن كلمة مرور Admin الافتراضية صحيحة - يُصحح تلقائياً إذا كانت Hash مكسورة
        /// </summary>
        private async Task EnsureAdminPasswordAsync(POS_BIGA.Services.IUserService userService)
        {
            try
            {
                const string defaultPassword = "admin123";

                // البحث عن أي مستخدم Admin (وليس بالضرورة Id=1)
                var allUsers = await userService.GetAllAsync();
                var admin = allUsers.FirstOrDefault(u => u.Role == POS_BIGA.Models.UserRole.Admin);

                if (admin == null)
                {
                    // لا يوجد أي Admin → أنشئ الافتراضي من جديد
                    Debug.WriteLine("⚠️ No admin found - creating default admin");
                    var newAdmin = new POS_BIGA.Models.AppUser
                    {
                        Username  = "admin",
                        FullName  = "مدير النظام",
                        Role      = POS_BIGA.Models.UserRole.Admin,
                        IsActive  = true,
                        CreatedBy = "System",
                    };
                    await userService.AddAsync(newAdmin, defaultPassword);
                    Debug.WriteLine("✅ Default admin created");
                    return;
                }

                // تحقق من صحة الـ Hash الحالي
                bool hashValid = false;
                try { hashValid = BCrypt.Net.BCrypt.Verify(defaultPassword, admin.PasswordHash); } catch { }

                if (!hashValid)
                {
                    Debug.WriteLine("⚠️ Admin password hash invalid - resetting");
                    await userService.ChangePasswordAsync(admin.Id, defaultPassword);
                    Debug.WriteLine("✅ Admin password reset successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureAdminPassword error: {ex.Message}");
            }
        }

        private void WriteLog(string text)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}");
            }
            catch { }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine($"DispatcherUnhandledException: {e.Exception}");
                WriteLog($"DispatcherUnhandledException: {e.Exception}");
                MessageBox.Show($"خطأ غير متوقع في واجهة المستخدم:\n{e.Exception.Message}\n\nانظر ملف السجل: {LogPath}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // prevent application from crashing
            }
            catch
            {
                // if showing message fails, ensure app doesn't rethrow
                e.Handled = true;
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                Debug.WriteLine($"CurrentDomain.UnhandledException: {ex}");
                WriteLog($"CurrentDomain.UnhandledException: {ex}");
                MessageBox.Show($"خطأ غير متوقع (خارج واجهة المستخدم):\n{ex?.Message}\n\nانظر ملف السجل: {LogPath}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Debug.WriteLine($"UnobservedTaskException: {e.Exception}");
                WriteLog($"UnobservedTaskException: {e.Exception}");
                e.SetObserved();
            }
            catch { }
        }
    }
}

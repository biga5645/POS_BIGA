using System;
using System.Collections.Generic;
using POS_BIGA.Models;

namespace POS_BIGA.Services
{
    /// <summary>
    /// ????? ???? ???????? ?????? � Static Singleton
    /// </summary>
    public static class SessionService
    {
        // ======================================
        // ?? ?????? ???????
        // ======================================

        public static AppUser? CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// الاسم المعروض للمستخدم الحالي المسجّل دخوله في التطبيق (وليس اسم حساب Windows).
        /// يُستخدم في كل سجلات التتبع (Cashier, CreatedBy, PerformedBy, ReceivedBy)
        /// حتى يمكن تمييز من قام فعلياً بكل عملية.
        /// </summary>
        public static string CurrentUserDisplayName =>
            CurrentUser?.FullName
            ?? CurrentUser?.Username
            ?? Environment.UserName
            ?? "System";

        /// <summary>????? ???? ???????? ????? ?? ??????</summary>
        public static void Login(AppUser user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        /// <summary>????? ???? ???????? ??????</summary>
        public static void Logout()
        {
            CurrentUser = null;
        }

        // ======================================
        // ?? ??? ????? ?????????
        // ======================================

        /// <summary>????? ??? ????? ????????? - ????? ???? ??? ViewModels ?????? ???????</summary>
        public static event EventHandler? PermissionsChanged;

        private static void RaisePermissionsChanged()
            => PermissionsChanged?.Invoke(null, EventArgs.Empty);

        // ======================================
        // ?? ?????? ?? ?????????
        // ======================================

        /// <summary>?? ???????? ?????? ???? ???????? ?????????</summary>
        public static bool HasPermission(Permission permission)
        {
            if (CurrentUser == null) return false;
            return _activePermissions.TryGetValue(CurrentUser.Role, out var perms)
                && perms.Contains(permission);
        }

        /// <summary>?? ???????? ?????? ?? ??? Admin?</summary>
        public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

        // ======================================
        // ?? ???? ????????? ????? (???? ????
        // ======================================

        private static Dictionary<UserRole, HashSet<Permission>> _activePermissions =
            BuildDefaultPermissions();

        /// <summary>????? ??????? ????? (?????? ?? ????? ????????)</summary>
        public static void ApplyCustomPermissions(Dictionary<UserRole, HashSet<Permission>> perms)
        {
            // Admin ?????? ???? ?? ?????????
            perms[UserRole.Admin] = new HashSet<Permission>(
                (Permission[])Enum.GetValues(typeof(Permission)));

            // ???? ?? ?? ??? ???? CanAccessSettings ??? ????
            foreach (var role in new[] { UserRole.Cashier, UserRole.Inventory })
            {
                if (perms.TryGetValue(role, out var set))
                    set.Add(Permission.CanAccessSettings);
            }

            _activePermissions = perms;
            RaisePermissionsChanged();
        }

        /// <summary>????? ????? ????????? ????? ??????????</summary>
        public static void ResetToDefaultPermissions()
        {
            _activePermissions = BuildDefaultPermissions();
            RaisePermissionsChanged();
        }

        /// <summary>??? ????????? ??????? ???? ???? (???????)</summary>
        public static IReadOnlySet<Permission> GetPermissionsForRole(UserRole role)
        {
            return _activePermissions.TryGetValue(role, out var set)
                ? set
                : new HashSet<Permission>();
        }

        private static Dictionary<UserRole, HashSet<Permission>> BuildDefaultPermissions() => new()
        {
            [UserRole.Admin] = new HashSet<Permission>(
                (Permission[])Enum.GetValues(typeof(Permission))),

            [UserRole.Cashier] = new HashSet<Permission>
            {
                Permission.CanSell,
                Permission.CanApplyDiscount,
                Permission.CanEditCartPrice,
                Permission.CanSuspendCart,
                Permission.CanViewProducts,
                Permission.CanViewCustomers,
                Permission.CanManageCredits,
                Permission.CanViewSalesHistory,
                Permission.CanAccessSettings,
            },

            [UserRole.Inventory] = new HashSet<Permission>
            {
                Permission.CanViewProducts,
                Permission.CanAddProduct,
                Permission.CanEditProduct,
                Permission.CanDeleteProduct,
                Permission.CanImportExportProducts,
                Permission.CanViewInventory,
                Permission.CanManageInventory,
                Permission.CanManageCategories,
                Permission.CanManageUnits,
                Permission.CanAccessSettings,
            },

            [UserRole.Viewer] = new HashSet<Permission>
            {
                Permission.CanViewProducts,
                Permission.CanViewReports,
                Permission.CanViewCustomers,
                Permission.CanViewInventory,
                Permission.CanViewSalesHistory,
            },
        };
    }
}

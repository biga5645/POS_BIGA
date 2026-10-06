using System;

namespace POS_BIGA.Services
{
    // Simple application-wide state notifier service.
    // - No static state
    // - Uses plain C# events
    // - Designed to be registered once (singleton) in DI or passed by constructor
    public interface IAppStateService
    {
        event EventHandler CategoriesChanged;
        event EventHandler UnitsChanged;
        event EventHandler ProductsChanged;
        event EventHandler CustomersChanged;
        event EventHandler CreditsChanged;
        event EventHandler InventoriesChanged;
        event EventHandler<string> PrintingStatusChanged;
        event Action<Models.Customer> CustomerUpdated;
        event EventHandler PrinterSettingsChanged;

        void NotifyCategoriesChanged();
        void NotifyUnitsChanged();
        void NotifyProductsChanged();
        void NotifyCustomersChanged();
        void NotifyCreditsChanged();
        void NotifyInventoriesChanged();
        void NotifyPrintingStatus(string message);
        void NotifyCustomerUpdated(Models.Customer customer);
        void NotifyPrinterSettingsChanged();

        // Suspended cart storage
        void AddSuspendedCart(Models.SuspendedCart cart);
        System.Collections.Generic.List<Models.SuspendedCart> ListSuspendedCarts();
        Models.SuspendedCart GetSuspendedCart(string id);
        void RemoveSuspendedCart(string id);
    }

    public class AppStateService : IAppStateService
    {
        public event EventHandler CategoriesChanged;
        public event EventHandler UnitsChanged;
        public event EventHandler ProductsChanged;
        public event EventHandler CustomersChanged;
        public event EventHandler CreditsChanged;
        public event EventHandler InventoriesChanged;
        public event EventHandler<string> PrintingStatusChanged;
        public event Action<Models.Customer> CustomerUpdated;
        public event EventHandler PrinterSettingsChanged;

        // Suspended carts storage
        private readonly System.Collections.Generic.List<Models.SuspendedCart> _suspendedCarts = new System.Collections.Generic.List<Models.SuspendedCart>();

        public void NotifyCategoriesChanged()
        {
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyUnitsChanged()
        {
            UnitsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyProductsChanged()
        {
            ProductsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyCustomersChanged()
        {
            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyCustomerUpdated(Models.Customer customer)
        {
            System.Diagnostics.Debug.WriteLine($"📢 AppStateService.NotifyCustomerUpdated: {customer?.FullName}");
            CustomerUpdated?.Invoke(customer);
        }

        public void NotifyPrinterSettingsChanged()
        {
            System.Diagnostics.Debug.WriteLine($"📢 AppStateService.NotifyPrinterSettingsChanged: إعادة تحميل إعدادات الطابعة");
            PrinterSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyCreditsChanged()
        {
            System.Diagnostics.Debug.WriteLine($"📢 AppStateService.NotifyCreditsChanged: إرسال إشعار تحديث الديون");
            System.Diagnostics.Debug.WriteLine($"   عدد المستمعين (Subscribers): {CreditsChanged?.GetInvocationList().Length ?? 0}");
            CreditsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"✓ تم إرسال إشعار CreditsChanged");
        }

        public void NotifyInventoriesChanged()
        {
            InventoriesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyPrintingStatus(string message)
        {
            try
            {
                PrintingStatusChanged?.Invoke(this, message ?? string.Empty);
            }
            catch { }
        }

        public void AddSuspendedCart(Models.SuspendedCart cart)
        {
            if (cart == null) return;
            _suspendedCarts.Add(cart);
        }

        public System.Collections.Generic.List<Models.SuspendedCart> ListSuspendedCarts()
        {
            return new System.Collections.Generic.List<Models.SuspendedCart>(_suspendedCarts);
        }

        public Models.SuspendedCart GetSuspendedCart(string id)
        {
            return _suspendedCarts.Find(c => c.Id == id);
        }

        public void RemoveSuspendedCart(string id)
        {
            var c = _suspendedCarts.Find(x => x.Id == id);
            if (c != null) _suspendedCarts.Remove(c);
        }
    }
}

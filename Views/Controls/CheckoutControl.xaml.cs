using POS_BIGA.Views.Windows;
using System;
using System.Windows;
using System.Windows.Controls;

namespace POS_BIGA.Views.Controls
{
    /// <summary>
    /// MVVM Strict - Minimal Code-Behind for Window Management
    /// </summary>
    public partial class CheckoutControl : UserControl
    {
        private QuickCashWindow _quickCashWindow;
        private Action<bool>? _requestCloseHandler;
        private Action? _requestQuickCashCloseHandler;
        private static DateTime _lastQuickCashClosed = DateTime.MinValue;
        private const int QuickCashCooldownMs = 500;
        private bool _isSmallLayoutApplied;

        public CheckoutControl()
        {
            InitializeComponent();
            DataContextChanged += CheckoutControl_DataContextChanged;
            Loaded += CheckoutControl_Loaded;
            SizeChanged += CheckoutControl_SizeChanged;
        }

        private void CheckoutControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old ViewModel
            if (e.OldValue is POS_BIGA.ViewModels.CheckoutViewModel oldVm)
            {
                oldVm.RequestQuickCash -= OpenQuickCash;
                if (_requestCloseHandler != null)
                    oldVm.RequestClose -= _requestCloseHandler;
                if (_requestQuickCashCloseHandler != null)
                    oldVm.RequestQuickCashClose -= _requestQuickCashCloseHandler;
            }

            // Subscribe to new ViewModel
            if (e.NewValue is POS_BIGA.ViewModels.CheckoutViewModel newVm)
            {
                newVm.RequestQuickCash += OpenQuickCash;
                _requestCloseHandler ??= _ => CloseQuickCashWindow();
                newVm.RequestClose += _requestCloseHandler;
                _requestQuickCashCloseHandler ??= () => CloseQuickCashWindow();
                newVm.RequestQuickCashClose += _requestQuickCashCloseHandler;
            }
        }

        private void CloseQuickCashWindow()
        {
            try { _quickCashWindow?.Close(); } catch { }
        }

        private void OpenQuickCash()
        {
            // respect cooldown after last close to avoid reopen loop
            try
            {
                if ((DateTime.UtcNow - _lastQuickCashClosed).TotalMilliseconds < QuickCashCooldownMs) return;
            }
            catch { }

            // prevent multiple windows
            if (_quickCashWindow != null)
            {
                try { _quickCashWindow.Focus(); } catch { }
                return;
            }

            _quickCashWindow = new QuickCashWindow();
            _quickCashWindow.Owner = Window.GetWindow(this);
            _quickCashWindow.Closed += QuickCashWindow_Closed;

            _quickCashWindow.Show();
        }

        private void QuickCashWindow_Closed(object sender, EventArgs e)
        {
            try { _lastQuickCashClosed = DateTime.UtcNow; } catch { }

            _quickCashWindow.Closed -= QuickCashWindow_Closed;
            _quickCashWindow = null;
        }

        /// <summary>
        /// إلغاء اختيار الزبون (تعيين زبون عام)
        /// </summary>
        private void ClearCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is POS_BIGA.ViewModels.CheckoutViewModel vm)
            {
                vm.SelectedCustomer = null;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckoutControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateResponsiveButtonsScale();
        }

        private void CheckoutControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateResponsiveButtonsScale();
        }

        private void UpdateResponsiveButtonsScale()
        {
            var isSmall = ActualWidth <= 520;
            if (_isSmallLayoutApplied == isSmall)
                return;

            _isSmallLayoutApplied = isSmall;

            var margin = isSmall ? new Thickness(2) : new Thickness(4);

            if (CancelButton != null)
            {
                CancelButton.Margin = margin;
                CancelButton.MinHeight = isSmall ? 52 : 64;
                CancelButton.FontSize = isSmall ? 16 : 20;
                CancelButton.Padding = isSmall ? new Thickness(10, 0, 10, 0) : new Thickness(20, 0, 20, 0);
            }

            if (SuspendCartButton != null)
            {
                SuspendCartButton.Margin = margin;
                SuspendCartButton.Height = isSmall ? 42 : 48;
                SuspendCartButton.FontSize = isSmall ? 15 : 20;
                SuspendCartButton.Padding = isSmall ? new Thickness(10, 0, 10, 0) : new Thickness(20, 0, 20, 0);
            }

            if (ShowSuspendedCartsButton != null)
            {
                ShowSuspendedCartsButton.Margin = margin;
                ShowSuspendedCartsButton.Height = isSmall ? 42 : 48;
                ShowSuspendedCartsButton.FontSize = isSmall ? 15 : 20;
                ShowSuspendedCartsButton.Padding = isSmall ? new Thickness(10, 0, 10, 0) : new Thickness(20, 0, 20, 0);
            }

            if (CompletePaymentButton != null)
            {
                CompletePaymentButton.Margin = margin;
                CompletePaymentButton.Height = isSmall ? 50 : 56;
                CompletePaymentButton.FontSize = isSmall ? 16 : 20;
                CompletePaymentButton.Padding = isSmall ? new Thickness(10, 0, 10, 0) : new Thickness(20, 0, 20, 0);
            }

            if (ActionButtonsGrid != null)
            {
                ActionButtonsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                ActionButtonsGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            }
        }
    }
}

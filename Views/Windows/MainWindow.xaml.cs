using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS_BIGA.Views.Windows;
using POS_BIGA.ViewModels;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // ✅ Dictionary لتتبع النوافذ المفتوحة (Singleton Pattern)
        private static readonly Dictionary<string, Window> _openWindows = new Dictionary<string, Window>();

        // debounce timer to avoid stealing focus during intentional interactions
        private DispatcherTimer _barcodeFocusDebounce;
        private DispatcherTimer _clockTimer;

        public MainWindow()
        {
            InitializeComponent();
            // Removed MainFrame navigation; keep MainViewModel as DataContext

            // ✨ التركيز التلقائي على خانة الباركود عند فتح النافذة
            Loaded += MainWindow_Loaded;
            Activated += MainWindow_Activated;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            _barcodeFocusDebounce = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(80) };
            _barcodeFocusDebounce.Tick += (s, e) =>
            {
                _barcodeFocusDebounce.Stop();
                FocusBarcodeInput();
            };

            // clock timer
            _clockTimer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClockDisplay();
            _clockTimer.Start();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // التركيز على خانة الباركود
            FocusBarcodeInput();

            // ✨ الاشتراك في event طلب التركيز على الباركود من ViewModel
            if (DataContext is MainViewModel vm)
            {
                vm.RequestBarcodeFocus += FocusBarcodeInput;

                // ✅ تحديد السطر المضاف/المحدَّث تلقائياً في سلة المشتريات
                vm.CartItemAdded += item =>
                {
                    CartDataGrid.SelectedItem = item;
                    CartDataGrid.ScrollIntoView(item);
                };

                // ✅ تحديد كل نص خانة البحث (SelectAll) عند الطلب من ViewModel
                vm.RequestSearchSelectAll += () =>
                {
                    SearchTextBox?.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
                    {
                        if (SearchTextBox == null) return;
                        SearchTextBox.Focus();
                        Keyboard.Focus(SearchTextBox);
                        SearchTextBox.SelectAll();
                    }));
                };

                // ✅ الاشتراك في تغيير خاصية إخفاء/إظهار عمود نوع السعر
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.IsPriceTypeColumnVisible))
                    {
                        PriceTypeColumn.Visibility = vm.IsPriceTypeColumnVisible
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                        RefreshCartColumnWidths();
                    }
                };

                // تطبيق الحالة الابتدائية
                PriceTypeColumn.Visibility = vm.IsPriceTypeColumnVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                RefreshCartColumnWidths();
            }
        }

        // عند استعادة النافذة الرئيسية التركيز (بعد إغلاق نافذة فرعية)
        private void MainWindow_Activated(object sender, System.EventArgs e)
        {
            // تأجيل بسيط لانتهاء تحريك النافذة قبل ضبط التركيز
            _barcodeFocusDebounce.Stop();
            _barcodeFocusDebounce.Start();
        }

        // اعتراض أي ضغطة لوحة مفاتيح إذا لم يكن التركيز على TextBox ←
        // يُعيد التركيز تلقائياً لخانة الباركود حتى يبدأ المستخدم الكتابة فوراً
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // --- إضافة اختصارات لوحة المفاتيح ---
            if (DataContext is MainViewModel vm)
            {
                // إتمام البيع مع طباعة (F1)
                if (e.Key == Key.F1)
                {
                    e.Handled = true;
                    if (vm.CheckoutCommand.CanExecute(null))
                        vm.CheckoutCommand.Execute(null);
                    return;
                }
                
                // تعليق السلة (F2)
                if (e.Key == Key.F2)
                {
                    e.Handled = true;
                    if (vm.SuspendCartCommand.CanExecute(null))
                        vm.SuspendCartCommand.Execute(null);
                    return;
                }

                 // فتح نافذة الدفع السريع والإنهاء بدون طباعة (F3) أو أي سلوك مخصص؟
                // سنكتفي بـ + و - حالياً لإدارة الكميات

                // زيادة الكمية لآخر عنصر أو تحديد أرقام سريعة
                if (e.Key == Key.Add || e.Key == Key.OemPlus)
                {
                    var item = GetSelectedOrFirstCartItem(vm);
                    if (item != null)
                    {
                        e.Handled = true;
                        decimal step = item.IsPiece ? 1m : 0.1m;
                        item.Quantity += step;
                        return;
                    }
                }

                // نقصان الكمية لآخر عنصر
                if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
                {
                    var item = GetSelectedOrFirstCartItem(vm);
                    if (item != null)
                    {
                        e.Handled = true;
                        decimal step = item.IsPiece ? 1m : 0.1m;

                        if (item.Quantity > step)
                            item.Quantity -= step;
                        else
                            vm.CartItems.Remove(item);
                        return;
                    }
                }
            }

            // تجاهل إذا كان التركيز بالفعل على TextBox
            if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is ComboBox)
                return;

            // تجاهل مفاتيح التنقل / الوظيفية / المعدِّلة
            if (e.Key == Key.Tab || e.Key == Key.Escape || e.Key == Key.Enter
                || e.Key == Key.F4 || e.Key == Key.F5
                || (e.Key >= Key.F1 && e.Key <= Key.F12)
                || Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                return;

            // أعد التركيز لخانة الباركود وابدأ الإدخال مباشرةً
            FocusBarcodeInput();
        }

        /// <summary>
        /// يُرجع العنصر المحدد في سلة المشتريات، وإن لم يكن هناك تحديد يُرجع أول عنصر.
        /// </summary>
        private Models.CartItem GetSelectedOrFirstCartItem(MainViewModel vm)
        {
            if (vm.CartItems.Count == 0) return null;

            if (CartDataGrid?.SelectedItem is Models.CartItem selected)
                return selected;

            return vm.CartItems[0];
        }

        /// <summary>
        /// إرجاع التركيز لخانة الباركود بأولوية Input لضمان التطبيق الفوري
        /// </summary>
        public void FocusBarcodeInput()
        {
            try
            {
                if (SearchTextBox == null) return;

                // استخدام Input priority لضمان التطبيق بعد انتهاء layout الحالي
                SearchTextBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    try
                    {
                        if (!SearchTextBox.IsLoaded) return;
                        SearchTextBox.Focus();
                        Keyboard.Focus(SearchTextBox);
                        SearchTextBox.SelectAll();
                    }
                    catch { }
                }));
            }
            catch { }
        }

        /// <summary>
        /// ✅ فتح نافذة واحدة فقط (Singleton Pattern)
        /// </summary>
        private void ShowSingleWindow<T>(string windowKey, System.Func<T> windowFactory) where T : Window
        {
            // التحقق إذا كانت النافذة مفتوحة بالفعل
            if (_openWindows.ContainsKey(windowKey))
            {
                var existingWindow = _openWindows[windowKey];
                if (existingWindow != null && existingWindow.IsLoaded)
                {
                    // إعادة التركيز على النافذة الموجودة
                    existingWindow.Activate();
                    existingWindow.Focus();
                    return;
                }
                else
                {
                    // إزالة النافذة المغلقة من Dictionary
                    _openWindows.Remove(windowKey);
                }
            }

            // إنشاء نافذة جديدة
            var window = windowFactory();
            // ربط بالنافذة الرئيسية لضمان بقاءها فوقها ووراثة الموضع
            window.Owner = this;
            // نضمن أن النافذة ستفتح في منتصف النافذة المالكة وتبقى خارج شريط المهام
            try
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            catch { }
            try { window.ShowInTaskbar = false; } catch { }
            try { window.ShowActivated = true; } catch { }
            // منع تصغير النافذة (إزالة زر التصغير) والاحتفاظ بزر الإغلاق فقط
            try
            {
                window.ResizeMode = ResizeMode.NoResize;
            }
            catch { }
            try
            {
                // SingleBorderWindow يضمن ظهور زر الإغلاق فقط مع إطار اعتيادي
                window.WindowStyle = WindowStyle.SingleBorderWindow;
            }
            catch { }

            // إضافة للـ Dictionary
            _openWindows[windowKey] = window;

            // حذف من Dictionary عند الإغلاق
            window.Closed += (s, e) =>
            {
                _openWindows.Remove(windowKey);
                // إرجاع التركيز للنافذة الرئيسية ثم لخانة الباركود
                this.Activate();
                FocusBarcodeInput();
            };

            // استخدام ShowDialog لجعل النافذة مُغلقة (modal) على النافذة الرئيسية
            try
            {
                window.ShowDialog();
            }
            catch
            {
                // Ensure cleanup even if ShowDialog throws for some reason
                try { window.Close(); } catch { }
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
            => OpenSettingsWindow();

        private void NavigateSettings_Click(object sender, RoutedEventArgs e)
            => OpenSettingsWindow();

        private void OpenSettingsWindow()
        {
            // التحقق إذا كانت النافذة مفتوحة بالفعل
            if (_openWindows.TryGetValue("Settings", out var existing) && existing?.IsLoaded == true)
            {
                existing.Activate();
                existing.Focus();
                return;
            }

            var win = new SettingsWindow { Owner = this };
            _openWindows["Settings"] = win;
            win.Closed += (_, _) =>
            {
                _openWindows.Remove("Settings");
                this.Activate();
                FocusBarcodeInput();
            };
            win.ShowDialog();
        }

        private void NavigateProducts_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SearchQuery = string.Empty;
            ProductsWindow.ShowSingle();
        }

        private void OpenCustomers_Click(object sender, RoutedEventArgs e)
        {
            ShowSingleWindow("Customers", () =>
            {
                var page = new POS_BIGA.Views.Pages.CustomersPage();
                return new Window
                {
                    Title = "إدارة الزبائن",
                    Content = page,
                    WindowState = WindowState.Maximized,
                    MinWidth = 1200,
                    MinHeight = 800,
                    FlowDirection = FlowDirection.RightToLeft,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
            });
        }

        private void OpenReports_Click(object sender, RoutedEventArgs e)
        {
            ShowSingleWindow("Reports", () =>
            {
                // clear any left embedded content
                if (DataContext is MainViewModel vm) { vm.LeftContent = null; vm.ShowCategories(); }
                return new POS_BIGA.Views.Windows.ReportsWindow();
            });
        }

        private void OpenInventory_Click(object sender, RoutedEventArgs e)
        {
            try { ShowSingleWindow("Inventory", () => new InventoryWindow()); }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Failed to open InventoryWindow: {ex}");
                MessageBox.Show($"حدث خطأ أثناء فتح نافذة إدارة المخزون:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyApiUrl_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                System.Windows.Clipboard.SetText(vm.ApiUrl);
                if (sender is Button btn)
                {
                    var icon = btn.Content as MaterialDesignThemes.Wpf.PackIcon;
                    if (icon != null)
                    {
                        icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Check;
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = System.TimeSpan.FromSeconds(2)
                        };
                        timer.Tick += (s, _) =>
                        {
                            icon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ContentCopy;
                            timer.Stop();
                        };
                        timer.Start();
                    }
                }
            }
        }

        private void CheckoutControl_Loaded(object sender, RoutedEventArgs e)
        {
            // subscribe to checkout VM close event so we can restore categories view after successful sale
            if (DataContext is MainViewModel vm && vm.CheckoutVM != null)
            {
                // remove previous subscription then add to avoid duplicates
                vm.CheckoutVM.RequestClose -= CheckoutVM_RequestClose;
                vm.CheckoutVM.RequestClose += CheckoutVM_RequestClose;
            }
        }

        private void CheckoutVM_RequestClose(bool success)
        {
            if (!success) return;

            // restore categories view on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DataContext is MainViewModel vm)
                    vm.ShowCategories();

                // تأجيل بسيط لإتاحة انتهاء تحريك واجهة الإتمام قبل التركيز
                Task.Delay(120).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(FocusBarcodeInput);
                });
            });
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SearchQuery = string.Empty;

            // set focus back to search box
            FocusBarcodeInput();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.CartItem item)
            {
                // تحديد الخطوة حسب نوع الوحدة
                decimal step = item.IsPiece ? 1m : 0.1m;
                item.Quantity += step;
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.CartItem item)
            {
                // تحديد الخطوة حسب نوع الوحدة
                decimal step = item.IsPiece ? 1m : 0.1m;

                if (item.Quantity > step)
                    item.Quantity -= step;
                else
                {
                    // Remove item from cart if quantity becomes 0
                    if (DataContext is MainViewModel vm)
                        vm.CartItems.Remove(item);
                }
            }
        }

        private void QuantityTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // السماح بالأرقام والنقطة العشرية فقط
            var textBox = sender as TextBox;
            var item = textBox?.Tag as Models.CartItem;
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // ✅ للقطع: منع النقطة العشرية
            if (item != null && item.IsPiece && e.Text == ".")
            {
                e.Handled = true;
                return;
            }

            // التحقق من صحة الإدخال (رقم مع نقطة عشرية اختيارية)
            if (!System.Text.RegularExpressions.Regex.IsMatch(fullText, @"^\d*\.?\d*$"))
            {
                e.Handled = true;
                return;
            }

            // منع نقاط عشرية متعددة
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }

            // التحقق من النطاق (حد أقصى: 9999.99)
            if (decimal.TryParse(fullText, out decimal value))
            {
                if (value > 9999.99m)
                {
                    e.Handled = true;
                }
            }
        }

        private void QuantityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // handled globally by TextBoxSelectAllBehavior
        }

        /// <summary>
        /// ✨ Validation لحقل الثمن المخصص (للوزن والمتر فقط)
        /// </summary>
        private void PriceTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            // السماح بالأرقام والنقطة العشرية فقط
            if (!System.Text.RegularExpressions.Regex.IsMatch(fullText, @"^\d*\.?\d*$"))
            {
                e.Handled = true;
                return;
            }

            // منع نقاط عشرية متعددة
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }

            // التحقق من النطاق
            if (decimal.TryParse(fullText, out decimal value))
            {
                if (value > 99999.99m)
                {
                    e.Handled = true;
                }
            }
        }

        private void PriceTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // handled globally by TextBoxSelectAllBehavior
        }

        private void UpdateClockDisplay()
        {
            try
            {
                var now = System.DateTime.Now;
                if (TimeText != null) TimeText.Text = now.ToString("HH:mm:ss");
                if (DateText != null) DateText.Text = now.ToString("yyyy-MM-dd");
            }
            catch { }
        }

        private void OpenOnScreenKeyboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Common candidate locations for TabTip and osk
                var candidates = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "Microsoft Shared", "ink", "TabTip.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Common Files", "Microsoft Shared", "ink", "TabTip.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Common Files", "Microsoft Shared", "ink", "TabTip.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "osk.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "osk.exe")
                };

                foreach (var p in candidates)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(p)) continue;
                        if (!File.Exists(p)) continue;

                        var psi = new ProcessStartInfo(p) { UseShellExecute = true };
                        var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            BringProcessToFront(proc);
                            return;
                        }
                    }
                    catch { }
                }

                // Try by process name fallback
                try { var p = Process.Start(new ProcessStartInfo("TabTip.exe") { UseShellExecute = true }); if (p != null) { BringProcessToFront(p); return; } } catch { }
                try { var p2 = Process.Start(new ProcessStartInfo("osk.exe") { UseShellExecute = true }); if (p2 != null) { BringProcessToFront(p2); return; } } catch { }

                // If already running, try to bring to front
                var existing = Process.GetProcessesByName("TabTip").FirstOrDefault() ?? Process.GetProcessesByName("osk").FirstOrDefault();
                if (existing != null) BringProcessToFront(existing);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Failed starting on-screen keyboard: {ex}");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(System.IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);

        private const int SW_SHOWNORMAL = 1;

        private void BringProcessToFront(Process proc)
        {
            try
            {
                if (proc == null) return;
                // ensure main window handle is ready
                proc.Refresh();
                var handle = proc.MainWindowHandle;
                if (handle == System.IntPtr.Zero)
                {
                    // give process a moment to create window
                    System.Threading.Thread.Sleep(100);
                    proc.Refresh();
                    handle = proc.MainWindowHandle;
                }
                if (handle != System.IntPtr.Zero)
                {
                    ShowWindow(handle, SW_SHOWNORMAL);
                    SetForegroundWindow(handle);
                }
            }
            catch { }
        }

        private void ProductsItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProductsColumns(e.NewSize.Width);
        }

        private void ProductsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // إعادة العدّ عند أي تمرير يدوي في قائمة المنتجات
            if (e.VerticalChange != 0 && DataContext is MainViewModel vm)
                vm.ResetReturnToCategoriesTimer();
        }

        private void ProductsArea_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // إعادة العدّ عند أي ضغط داخل منطقة المنتجات (ضغط على منتج، تمرير، إلخ)
            if (DataContext is MainViewModel vm)
                vm.ResetReturnToCategoriesTimer();
        }

        private void UpdateProductsColumns(double availableWidth = -1)
        {
            try
            {
                if (ProductsItemsControl == null)
                    return;

                var wrap = FindVisualChild<WrapPanel>(ProductsItemsControl);
                if (wrap == null)
                    return;

                if (availableWidth <= 0)
                    availableWidth = ProductsItemsControl.ActualWidth;

                const double minCardWidth = 185d;
                const double itemHorizontalMargin = 12d; // Margin=6 يمين + 6 يسار

                var columns = System.Math.Max(1, (int)System.Math.Floor(availableWidth / minCardWidth));
                var itemWidth = (availableWidth / columns) - itemHorizontalMargin;

                wrap.ItemWidth = System.Math.Max(minCardWidth - itemHorizontalMargin, itemWidth);
                wrap.ItemHeight = double.NaN;
            }
            catch { }
        }

        private void CartDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshCartColumnWidths(e.NewSize.Width);
        }

        private void RefreshCartColumnWidths(double width = -1)
        {
            try
            {
                if (CartDataGrid == null) return;

                double availableWidth = width > 0 ? width : CartDataGrid.ActualWidth;
                bool priceTypeHidden = PriceTypeColumn.Visibility == Visibility.Collapsed;
                bool compact = availableWidth <= 560;

                if (compact)
                {
                    CartDataGrid.FontSize = 11;
                    CartDataGrid.RowHeight = 42;
                    // عند إخفاء عمود نوع السعر نعيد توزيع العرض المحرَّر (84px) على المنتج والكمية
                    ProductNameColumn.Width  = new DataGridLength(priceTypeHidden ? 2.2 : 1.8, DataGridLengthUnitType.Star);
                    PriceTypeColumn.Width    = new DataGridLength(84);
                    QuantityColumn.Width     = new DataGridLength(priceTypeHidden ? 116 : 96);
                    CustomPriceColumn.Width  = new DataGridLength(72);
                    TotalColumn.Width        = new DataGridLength(78);
                }
                else
                {
                    CartDataGrid.FontSize = 13;
                    CartDataGrid.RowHeight = 48;
                    // عند إخفاء عمود نوع السعر نعيد توزيع العرض المحرَّر (110px) على المنتج والكمية
                    ProductNameColumn.Width  = new DataGridLength(1, DataGridLengthUnitType.Star);
                    PriceTypeColumn.Width    = new DataGridLength(110);
                    QuantityColumn.Width     = new DataGridLength(priceTypeHidden ? 150 : 120);
                    CustomPriceColumn.Width  = new DataGridLength(priceTypeHidden ? 110 : 90);
                    TotalColumn.Width        = new DataGridLength(priceTypeHidden ? 105 : 85);
                }
            }
            catch { }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }
    }
}

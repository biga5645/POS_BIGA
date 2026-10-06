using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.ComponentModel;

namespace POS_BIGA.Views.Windows
{
    public partial class QuickCashWindow : Window
    {
        public decimal SelectedAmount { get; private set; }
        public string SelectedMethod { get; private set; }

        private readonly string _assetsPath;
        private Window? _ownerWindow;
        private bool _isClosingInternally;

        public QuickCashWindow()
        {
            InitializeComponent();
            _assetsPath = LocateAssetsFolder() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cash");
            SelectedAmount = 0m;
            PreviewKeyDown += QuickCashWindow_PreviewKeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var sw = SystemParameters.WorkArea.Width;
            var sh = SystemParameters.WorkArea.Height;

            // حجم متكيف: 75% من الشاشة كحد أقصى مع حد أدنى مناسب
            Width  = Math.Max(360, Math.Min(860, sw * 0.75));
            Height = Math.Max(320, Math.Min(520, sh * 0.70));

            // تكييف عدد أعمدة الأزرار حسب العرض
            if (Width < 480)
                CashButtonsGrid.Columns = 2;
            else if (Width < 640)
                CashButtonsGrid.Columns = 3;
            else
                CashButtonsGrid.Columns = 4;

            _ownerWindow = Owner;
            if (_ownerWindow != null)
            {
                _ownerWindow.LocationChanged += OwnerWindow_Changed;
                _ownerWindow.SizeChanged     += OwnerWindow_Changed;
                _ownerWindow.Closing         += OwnerWindow_Closing;
            }

            PositionRelativeToOwner();

            try
            {
                var owner = this.Owner as Window;
                if (owner != null)
                {
                    var dc = owner.DataContext;
                    if (dc != null)
                    {
                        SetImageFromParent(dc, "Cash0_5Image", img0_5);
                        SetImageFromParent(dc, "Cash1Image",   img1);
                        SetImageFromParent(dc, "Cash2Image",   img2);
                        SetImageFromParent(dc, "Cash5Image",   img5);
                        SetImageFromParent(dc, "Cash10Image",  img10);
                        SetImageFromParent(dc, "Cash20Image",  img20);
                        SetImageFromParent(dc, "Cash50Image",  img50);
                        SetImageFromParent(dc, "Cash100Image", img100);
                        SetImageFromParent(dc, "Cash200Image", img200);
                    }
                }
            }
            catch { }

            LoadImages();
            UpdatePreview();
        }

        private void OwnerWindow_Changed(object? sender, EventArgs e)
        {
            PositionRelativeToOwner();
        }

        private void OwnerWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                _isClosingInternally = true;
                Close();
            }
            catch { }
        }

        private void PositionRelativeToOwner()
        {
            try
            {
                var workArea = SystemParameters.WorkArea;
                double sw = workArea.Width;
                double sh = workArea.Height;

                double targetLeft, targetTop;

                if (_ownerWindow == null)
                {
                    targetLeft = (sw - Width) / 2;
                    targetTop  = sh - Height - 12;
                }
                else
                {
                    // في المنتصف أسفل النافذة الأم
                    targetLeft = _ownerWindow.Left + (_ownerWindow.ActualWidth  - Width)  / 2;
                    targetTop  = _ownerWindow.Top  +  _ownerWindow.ActualHeight - Height - 12;
                }

                // ضمان بقاء النافذة داخل حدود الشاشة
                Left = Math.Max(workArea.Left + 8, Math.Min(targetLeft, workArea.Right  - Width  - 8));
                Top  = Math.Max(workArea.Top  + 8, Math.Min(targetTop,  workArea.Bottom - Height - 8));
            }
            catch { }
        }

        private void QuickCashWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Confirm_Click(this, new RoutedEventArgs());
            }
            // زيادة آخر مبلغ أو إعداد اختصارات للعملات
            else if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                e.Handled = true;
                // كمثال: تكرار آخر عملية أو إضافة مبلغ افتراضي
                // سنقوم باستدعاء زر افتراضي مثلاً أو تجاهل لأن الأزرار تعتمد على الماوس بالأساس
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                e.Handled = true;
                // تقليل المبلغ أو تصفيره
                Reset_Click(this, new RoutedEventArgs());
            }
        }

        private void SetImageFromParent(object dc, string propName, Image target)
        {
            try
            {
                var p = dc.GetType().GetProperty(propName);
                if (p != null)
                {
                    var val = p.GetValue(dc);
                    if (val is ImageSource img)
                    {
                        target.Source = img;
                    }
                }
            }
            catch { }
        }

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag != null)
            {
                // parse tag using current culture to match displayed numbers
                if (decimal.TryParse(b.Tag.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amt))
                {
                    // Try to find AmountTextBox in owner and update it (simulate typing)
                    try
                    {
                        var owner = this.Owner;
                        if (owner != null)
                        {
                            var tb = FindChild<TextBox>(owner, "AmountTextBox");
                            if (tb != null)
                            {
                                // determine culture from textbox language and parse/format consistently
                                CultureInfo tbCulture;
                                try { tbCulture = new CultureInfo(tb.Language.IetfLanguageTag); } catch { tbCulture = CultureInfo.CurrentCulture; }
                                // parse current value using textbox culture
                                decimal current = 0m;
                                if (!string.IsNullOrWhiteSpace(tb.Text))
                                    decimal.TryParse(tb.Text, NumberStyles.Number, tbCulture, out current);
                                var newVal = decimal.Round(current + amt, 2);
                                tb.Text = newVal.ToString("N2", tbCulture);

                                 // also try to update DataContext.AmountPaid if exists
                                 try
                                 {
                                     var dc = owner.DataContext;
                                     if (dc != null)
                                     {
                                         var prop = dc.GetType().GetProperty("AmountPaid");
                                         if (prop != null && prop.CanWrite)
                                         {
                                             prop.SetValue(dc, Convert.ChangeType(newVal, prop.PropertyType));
                                         }
                                     }
                                 }
                                 catch { }

                                 // تحديث المعاينة داخل نافذة الدفع السريع بدون تغيير التركيز
                                 SelectedAmount = newVal;
                                 UpdatePreview();

                                 // DO NOT close the quickcash window here; allow multiple presses until Confirm
                                 return;
                             }

                             // if no textbox, try setting DataContext.AmountPaid
                             var dcFallback = owner.DataContext;
                             if (dcFallback != null)
                             {
                                 var prop = dcFallback.GetType().GetProperty("AmountPaid");
                                 if (prop != null && prop.CanWrite)
                                 {
                                     // get current
                                     decimal current = 0m;
                                     try { var val = prop.GetValue(dcFallback); if (val != null) current = Convert.ToDecimal(val); } catch { }
                                     var newVal = current + amt;
                                    try { prop.SetValue(dcFallback, Convert.ChangeType(newVal, prop.PropertyType)); } catch { }
                                    SelectedAmount = decimal.Round(newVal, 2);
                                    UpdatePreview();
                                     // DO NOT close; just return after applying
                                     return;
                                 }
                             }
                         }
                     }
                     catch { }

                     // fallback: accumulate internally
                     SelectedAmount += amt;
                     SelectedMethod = "Cash";
                     UpdatePreview();
                 }
             }
         }

        // helper: find child by name and type in visual tree
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    if (child is FrameworkElement fe && fe.Name == childName) return typedChild;
                }

                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        private void UpdatePreview()
        {
            try
            {
                if (PreviewText != null)
                    PreviewText.Text = SelectedAmount.ToString("N2", CultureInfo.CurrentCulture) + " ";
            }
            catch { if (PreviewText != null) PreviewText.Text = SelectedAmount.ToString(); }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            // simply close and keep current applied amounts in owner
            _isClosingInternally = true;
            this.Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            try
            {
                // لا تغلق النافذة تلقائياً عند فقدان التركيز داخل نفس التطبيق.
                // الإغلاق يكون فقط من زر الإغلاق / Esc / إتمام أو إلغاء البيع.
                if (!IsVisible) return;
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _isClosingInternally = true;
            this.Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var owner = Owner;
                if (owner != null)
                {
                    var tb = FindChild<TextBox>(owner, "AmountTextBox");
                    if (tb != null)
                    {
                        CultureInfo tbCulture;
                        try { tbCulture = new CultureInfo(tb.Language.IetfLanguageTag); } catch { tbCulture = CultureInfo.CurrentCulture; }
                        tb.Text = 0m.ToString("N2", tbCulture);
                    }

                    var dc = owner.DataContext;
                    if (dc != null)
                    {
                        var prop = dc.GetType().GetProperty("AmountPaid");
                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(dc, Convert.ChangeType(0m, prop.PropertyType));
                        }
                    }
                }
            }
            catch { }

            SelectedAmount = 0m;
            UpdatePreview();
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                PreviewKeyDown -= QuickCashWindow_PreviewKeyDown;
                if (_ownerWindow != null)
                {
                    _ownerWindow.LocationChanged -= OwnerWindow_Changed;
                    _ownerWindow.SizeChanged -= OwnerWindow_Changed;
                    _ownerWindow.Closing -= OwnerWindow_Closing;
                }
            }
            catch { }

            base.OnClosed(e);
        }

        // helper methods restored
        private string LocateAssetsFolder()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "Assets");
                    if (Directory.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
            }
            catch { }
            return null;
        }

        private void LoadImages()
        {
            TryLoadImage(new[] { "cash0_5.png", "cash0.5.png", "cash_0.png", "0_5.png", "0.5.png", "0_5dh.png", "0_5dirham.png" }, img0_5);

            TryLoadImage(new[] { "cash1.png", "cash_1.png", "1.png", "1dh.png", "1dirham.png", "dirham1.png", "cash1dh.png" }, img1);

            TryLoadImage(new[] { "cash2.png", "cash_2.png", "2.png", "2dh.png", "2dirham.png", "dirham2.png", "cash2dh.png" }, img2);

            TryLoadImage(new[] { "cash5.png", "cash_5.png", "5.png", "5dh.png", "5dirham.png", "dirham5.png" }, img5);

            TryLoadImage(new[] { "cash10.png", "cash_10.png", "10.png", "10dh.png", "10dirham.png", "dirham10.png" }, img10);

            TryLoadImage(new[] { "cash20.png", "cash_20.png", "20.png", "20dh.png", "20dirham.png", "dirham20.png" }, img20);

            TryLoadImage(new[] { "cash50.png", "cash_50.png", "50.png", "50dh.png", "50dirham.png", "dirham50.png" }, img50);

            TryLoadImage(new[] { "cash100.png", "cash_100.png", "100.png", "100dh.png", "100dirham.png", "dirham100.png" }, img100);

            TryLoadImage(new[] { "cash200.png", "cash_200.png", "200.png", "200dh.png", "200dirham.png", "dirham200.png" }, img200);
        }

        private void TryLoadImage(string fileName, Image target)
        {
            try
            {
                // try direct path in discovered assets folder
                if (!string.IsNullOrEmpty(_assetsPath))
                {
                    var full = Path.Combine(_assetsPath, fileName);
                    if (File.Exists(full))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(full, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        target.Source = bmp;
                        return;
                    }
                }

                // fallback: try app base directory Assets
                var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
                if (File.Exists(fallback))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(fallback, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    target.Source = bmp;
                    return;
                }

                // final fallback: if given as resource (pack URI) try loading via pack
                try
                {
                    var packUri = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
                    var bmp2 = new BitmapImage(packUri);
                    target.Source = bmp2;
                    return;
                }
                catch { }

                target.Source = null;
            }
            catch { target.Source = null; }
        }

        private void TryLoadImage(System.Collections.Generic.IEnumerable<string> fileNames, Image target)
        {
            foreach (var fn in fileNames)
            {
                TryLoadImage(fn, target);
                if (target.Source != null) return;
            }
        }
    }
}
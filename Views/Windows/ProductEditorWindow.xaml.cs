using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using POS_BIGA.Models;
using POS_BIGA.Data.Repositories;
using System.Threading.Tasks;
using POS_BIGA.Views.Pages;
using POS_BIGA.Infrastructure.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace POS_BIGA.Views.Windows
{
    public partial class ProductEditorWindow : Window
    {
        public Product Product { get; private set; }
        private bool _isEmbeddedMode;
        // Pending local image path selected by user but not yet saved to central Images folder
        private string? _pendingLocalImagePath;
        // Keep original image url to restore on cancel if user previewed a local image
        private string? _originalImageUrl;
        public bool IsEmbeddedMode
        {
            get => _isEmbeddedMode;
            set
            {
                _isEmbeddedMode = value;
                ApplyEmbeddedLayout();
            }
        }

        private Thickness _defaultRootMargin;
        private GridLength _defaultHeaderRowHeight;
        public event Action<Product>? SaveRequested;
        public event Action? CancelRequested;

        public ProductEditorWindow()
        {
            InitializeComponent();
            _defaultRootMargin = RootGrid.Margin;
            _defaultHeaderRowHeight = HeaderRow.Height;
            Loaded += (_, _) =>
            {
                ApplyEmbeddedLayout();
            };
            Product = new Product();
            _originalImageUrl = Product.ImageUrl;
            DataContext = Product;

            // ✅ التهيئة تعمل سواء كنافذة مستقلة أو Embedded
            _ = InitializeDataAsync();
        }

        public ProductEditorWindow(Product existing)
        {
            InitializeComponent();
            _defaultRootMargin = RootGrid.Margin;
            _defaultHeaderRowHeight = HeaderRow.Height;
            Loaded += (_, _) =>
            {
                ApplyEmbeddedLayout();
            };
            Product = existing ?? new Product();
            _originalImageUrl = Product.ImageUrl;
            DataContext = Product;
            _ = InitializeDataAsync();
        }

        private void ApplyEmbeddedLayout()
        {
            if (!IsInitialized) return;

            if (_isEmbeddedMode)
            {
                RootGrid.Margin = new Thickness(6);
                HeaderRow.Height = new GridLength(0);
                HeaderSection.Visibility = Visibility.Collapsed;
            }
            else
            {
                RootGrid.Margin = _defaultRootMargin;
                HeaderRow.Height = _defaultHeaderRowHeight;
                HeaderSection.Visibility = Visibility.Visible;
            }
        }

        private async Task InitializeDataAsync()
        {
            // ✅ تحميل الفئات والوحدات أولاً
            await Task.WhenAll(LoadCategories(), LoadUnits());

            // ✅ تعيين DataContext بعد تحميل البيانات
            DataContext = Product;

            // ⏱️ انتظار قليلاً لضمان تطبيق Binding
            await Task.Delay(10);

            // ✅ تعيين الوحدة المحددة بعد التحميل الكامل
            if (Product.UnitId.HasValue)
            {
                UnitCombo.SelectedValue = Product.UnitId.Value;
                System.Diagnostics.Debug.WriteLine($"✅ Setting UnitCombo.SelectedValue to: {Product.UnitId.Value}");

                // ✅ تأكيد التعيين
                await Dispatcher.InvokeAsync(() =>
                {
                    if (UnitCombo.SelectedValue == null || (int)UnitCombo.SelectedValue != Product.UnitId.Value)
                    {
                        // إعادة المحاولة
                        UnitCombo.SelectedValue = Product.UnitId.Value;
                        System.Diagnostics.Debug.WriteLine($"🔄 Retry setting UnitCombo.SelectedValue to: {Product.UnitId.Value}");
                    }
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            // ✅ لا حاجة لـ fallback - نعتمد فقط على UnitId
        }

        private async Task LoadCategories()
        {
            try
            {
                var repo = new CategoryRepository();
                var cats = await repo.GetAllCategoriesAsync();
                CategoryCombo.ItemsSource = cats;
            }
            catch { }
        }

        private async Task LoadUnits()
        {
            try
            {
                var repo = new UnitRepository();
                var units = await repo.GetAllUnitsAsync();
                UnitCombo.ItemsSource = units;
                System.Diagnostics.Debug.WriteLine($"✅ Loaded {units.Count} units");
            }
            catch { }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Ensure current bindings are pushed to source
            try
            {
                // Parse price TextBoxes to ensure decimals are interpreted correctly regardless of binding timing
                void ParsePriceBox(TextBox tb, Action<decimal> assign, bool allowNull = false)
                {
                    if (tb == null) return;
                    var text = (tb.Text ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(text))
                    {
                        if (allowNull)
                        {
                            assign(0m);
                        }
                        else
                        {
                            assign(0m);
                        }
                        return;
                    }

                    // normalize to '.' and parse using invariant culture to avoid culture-specific misparsing
                    var normalized = text.Replace(',', '.');
                    normalized = Regex.Replace(normalized, @"[^0-9.\-]", string.Empty);
                    System.Diagnostics.Debug.WriteLine($"[SaveParse] Text='{text}' Normalized='{normalized}' Culture='{CultureInfo.CurrentCulture.Name}'");
                    if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                    {
                        assign(v);
                        System.Diagnostics.Debug.WriteLine($"[SaveParse] Parsed decimal: {v}");
                    }
                }

                // apply parsing to all price fields before updating focus/binding
                ParsePriceBox(PriceBox, v => Product.Price = v, false);
                ParsePriceBox(PurchasePriceBox, v => Product.PurchasePrice = v, false);
                ParsePriceBox(WholesalePriceBox, v => Product.WholesalePrice = v, true);
                ParsePriceBox(InstallmentPriceBox, v => Product.InstallmentPrice = v, true);
                System.Diagnostics.Debug.WriteLine($"[Save_Click] Product.Price={Product.Price}, SalePrice={Product.SalePrice}, PurchasePrice={Product.PurchasePrice}, Wholesale={Product.WholesalePrice}, Installment={Product.InstallmentPrice}");

                // try
                // {
                //     // ✅ تأكيد حفظ الوحدة المختارة قبل الإغلاق
                //     if (UnitCombo.SelectedValue is int unitId)
                //     {
                //         Product.UnitId = unitId;
                //         Product.UnitEntity = null;
                //     }
                //     else
                //     {
                //         Product.UnitId = null;
                //         Product.UnitEntity = null;
                //     }

                //     var scope = FocusManager.GetFocusScope(this);
                //     FocusManager.SetFocusedElement(scope, this);
                //     Keyboard.ClearFocus();
                // }
                // catch { }

                // If user selected a local image but didn't save it to central folder yet, copy it now
                if (!string.IsNullOrWhiteSpace(_pendingLocalImagePath))
                {
                    try
                    {
                        var preferredName = !string.IsNullOrWhiteSpace(Product?.Name) ? Product.Name : Product?.Barcode;
                        // ✅ يُرجع /Images/Products/اسم.jpg مباشرة
                        Product.ImageUrl = ImageStorageHelper.SaveToCentralFolder(_pendingLocalImagePath, "Products", preferredName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في حفظ صورة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        _pendingLocalImagePath = null;
                    }
                }
                else if (string.IsNullOrWhiteSpace(Product?.ImageUrl))
                {
                    // ✅ لا توجد صورة مرفوعة → نُنشئ مسار افتراضي تلقائياً بدون نسخ أي ملف
                    var nameForPath = !string.IsNullOrWhiteSpace(Product?.Name) ? Product.Name : Product?.Barcode;
                    if (!string.IsNullOrWhiteSpace(nameForPath))
                        Product.ImageUrl = ImageStorageHelper.BuildDefaultImagePath(nameForPath, "Products");
                }

                if (IsEmbeddedMode)
                {
                    SaveRequested?.Invoke(Product);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch { }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // If user previewed a local image but didn't save, restore original image value
            if (!string.IsNullOrWhiteSpace(_pendingLocalImagePath))
            {
                Product.ImageUrl = _originalImageUrl ?? string.Empty;
                _pendingLocalImagePath = null;
                DataContext = null;
                DataContext = Product;
            }

            if (IsEmbeddedMode)
            {
                CancelRequested?.Invoke();
                return;
            }

            DialogResult = false;
            Close();
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختر صورة المنتج",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // Preview the selected local file immediately without copying
                    _pendingLocalImagePath = dlg.FileName;
                    // Set ImageUrl to the local file path so the image converter can display it for preview
                    Product.ImageUrl = dlg.FileName;
                    DataContext = null;
                    DataContext = Product;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حفظ صورة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UnitCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                var u = UnitCombo.SelectedItem as Unit;
                if (u != null)
                {
                    Product.UnitId = u.Id;
                    // ✅ لا حاجة لـ Product.Unit - نعتمد فقط على UnitId
                    System.Diagnostics.Debug.WriteLine($"✅ Unit changed to: {u.Abbreviation} (Id: {u.Id})");
                }
                else
                {
                    Product.UnitId = null;
                }
            }
            catch { }
        }

        private void OpenUnits_Click(object sender, RoutedEventArgs e)
        {
            var page = new UnitsPage();
            var win = new Window { Title = "إدارة الوحدات", Content = page, Owner = Window.GetWindow(this), Width = 700, Height = 480 };
            win.ShowDialog();

            // reload units after potential changes
            _ = LoadUnits();
        }

        private void BarcodeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Product == null) return;
            NormalizeBarcode();
        }

        // ── تطبيع حرف بحرف أثناء الكتابة (لوحة فرنسية/عربية) ──
        private void BarcodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;

            var normalized = BarcodeHelper.FixBarcodeText(e.Text);
            if (normalized == e.Text) return; // لا تغيير ← اترك WPF يُدخله بشكل طبيعي

            // استبدل الحرف المُدخَل بنسخته المُصحَّحة
            e.Handled = true;
            var tb = (TextBox)sender;
            var caret = tb.CaretIndex;
            if (tb.SelectionLength > 0)
                tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);

            tb.Text = tb.Text.Insert(tb.CaretIndex, normalized);
            tb.CaretIndex = caret + normalized.Length;

            // تحديث الـ Binding يدوياً
            if (Product != null) Product.Barcode = tb.Text;
        }

        // ── تطبيع عند لصق (Ctrl+V) ──
        private void BarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                var tb = (TextBox)sender;
                var pasted = Clipboard.GetText() ?? string.Empty;
                var normalized = BarcodeHelper.Normalize(pasted); // trim + fix
                if (tb.SelectionLength > 0)
                    tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
                tb.Text = tb.Text.Insert(tb.CaretIndex, normalized);
                tb.CaretIndex = tb.Text.Length;
                if (Product != null) Product.Barcode = tb.Text;
                return;
            }

            // ── مسح فوري بـ Escape ──
            if (e.Key == Key.Escape)
            {
                ((TextBox)sender).Clear();
                if (Product != null) Product.Barcode = string.Empty;
                e.Handled = true;
            }
        }

        private void NormalizeBarcode()
        {
            if (string.IsNullOrEmpty(Product?.Barcode)) return;
            var corrected = BarcodeHelper.Normalize(Product.Barcode);
            if (corrected == Product.Barcode) return;
            Product.Barcode = corrected;
            // تحديث TextBox
            if (BarcodeTextBox != null) BarcodeTextBox.Text = corrected;
        }

        // Allow decimal numeric input (digits and a single decimal separator) for wholesale/installment boxes
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var input = e.Text ?? string.Empty;
            if (string.IsNullOrEmpty(input)) return;

            // السماح بالأرقام فقط
            if (Regex.IsMatch(input, "^[0-9]$"))
                return;

            // السماح بالفاصل العشري (نقطة أو فاصلة) — دائماً نُدرج '.'
            if (input == "." || input == ",")
            {
                if (tb.Text.Contains('.'))
                {
                    e.Handled = true;
                    return;
                }
                e.Handled = true;
                var caret = tb.CaretIndex;
                if (tb.SelectionLength > 0)
                    tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
                tb.Text = tb.Text.Insert(caret, ".");
                tb.CaretIndex = caret + 1;
                return;
            }

            // حظر أي حرف آخر
            e.Handled = true;
        }

        private void NumericTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText) && !e.SourceDataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pasted = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string ?? e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pasted))
            {
                e.CancelCommand();
                return;
            }

            // normalize commas to current culture decimal separator
            var decimalSep = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            var normalized = pasted.Trim();
            if (decimalSep == ",")
            {
                normalized = normalized.Replace('.', ',');
            }
            else
            {
                normalized = normalized.Replace(',', '.');
            }

            // allow only digits and at most one decimal separator
            if (!Regex.IsMatch(normalized, "^[0-9]+(" + Regex.Escape(decimalSep) + "[0-9]*)?$"))
            {
                e.CancelCommand();
                return;
            }

            // perform insertion manually
            e.CancelCommand();
            var caret = tb.CaretIndex;
            if (tb.SelectionLength > 0)
                tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
            tb.Text = tb.Text.Insert(tb.CaretIndex, normalized);
            tb.CaretIndex = caret + normalized.Length;
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            // allow navigation and control keys
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Tab || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Enter)
                return;

            // مفاتيح النقطة والفاصلة — دائماً نُدرج '.'
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal || e.Key == Key.OemComma)
            {
                if (!tb.Text.Contains('.'))
                {
                    var caret = tb.CaretIndex;
                    if (tb.SelectionLength > 0)
                        tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
                    tb.Text = tb.Text.Insert(caret, ".");
                    tb.CaretIndex = caret + 1;
                }
                e.Handled = true;
            }
        }

        private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var text = (tb.Text ?? string.Empty).Trim();

            // empty -> clear nullable fields or set 0 for required
            if (string.IsNullOrEmpty(text))
            {
                if (tb == WholesalePriceBox) Product.WholesalePrice = null;
                else if (tb == InstallmentPriceBox) Product.InstallmentPrice = null;
                else if (tb == PriceBox) Product.Price = 0m;
                else if (tb == PurchasePriceBox) Product.PurchasePrice = 0m;
                return;
            }

            // Normalize decimal separator to invariant (use '.'), remove other characters, then parse with InvariantCulture
            var normalized = text.Replace(',', '.');
            // Remove any characters except digits, dot and leading -
            normalized = Regex.Replace(normalized, @"[^0-9.\-]", string.Empty);
            System.Diagnostics.Debug.WriteLine($"[NumericLostFocus] Text='{text}' Normalized='{normalized}' Culture='{CultureInfo.CurrentCulture.Name}'");

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
            {
                if (tb == WholesalePriceBox)
                    Product.WholesalePrice = val;
                else if (tb == InstallmentPriceBox)
                    Product.InstallmentPrice = val;
                else if (tb == PriceBox)
                    Product.Price = val;
                else if (tb == PurchasePriceBox)
                    Product.PurchasePrice = val;

                // ✅ دائماً نستخدم InvariantCulture حتى لا يُحوَّل الفاصل العشري إلى فاصلة
                tb.Text = val.ToString("0.##", CultureInfo.InvariantCulture);
                try
                {
                    var be = tb.GetBindingExpression(TextBox.TextProperty);
                    be?.UpdateSource();
                }
                catch { }
            }
            else
            {
                // if parsing failed, leave the text as-is but do not update the model
            }
        }

        /// <summary>
        /// يُعلّق حدث GotFocus على كل TextBox في النافذة لتحديد النص تلقائياً عند التركيز
        /// </summary>
        private void AttachSelectAllToTextBoxes()
        {
            try
            {
                var root = Content as DependencyObject;
                if (root == null) return;
                foreach (var tb in FindVisualChildren<TextBox>(root))
                {
                    if (tb.IsReadOnly) continue;
                    tb.GotFocus -= TextBox_GotFocus;
                    tb.GotFocus += TextBox_GotFocus;
                    tb.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseDown;
                    tb.PreviewMouseLeftButtonDown += TextBox_PreviewMouseDown;
                }
            }
            catch { }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                // تأجيل بسيط لضمان اكتمال routing قبل التحديد
                tb.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(tb.SelectAll));
            }
        }

        // Prevent losing focus selection when clicking on unfocused text box
        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root == null) yield break;
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;
                foreach (var desc in FindVisualChildren<T>(child))
                    yield return desc;
            }
        }
    }
}

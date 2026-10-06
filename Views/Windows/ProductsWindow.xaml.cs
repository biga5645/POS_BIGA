using System.Windows;
using POS_BIGA.ViewModels;
using POS_BIGA.Views.Pages;
using System.Collections.Generic;
using System.Windows.Controls;
using POS_BIGA.Services;
using POS_BIGA.Models;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Text.Json;

namespace POS_BIGA.Views.Windows
{
    public partial class ProductsWindow : Window
    {
        // ✅ نافذة واحدة فقط (Singleton)
        private static ProductsWindow _instance;
        private readonly ProductService _productService = new ProductService();
        private readonly IAppSettingService _appSettingService = new AppSettingService();
        private const string ProductsColumnsSettingKey = "UI.ProductsGrid.VisibleColumns";
        private ProductEditorWindow? _embeddedProductEditor;
        private bool _isEditMode;

        public ProductsWindow()
        {
            InitializeComponent();

            // ✅ إزالة من Singleton عند الإغلاق
            Closed += (s, e) =>
            {
                _instance = null;
                // إعادة التركيز للنافذة الرئيسية
                Application.Current?.MainWindow?.Activate();
            };

            // عند تحميل النافذة ضع التركيز على مربع البحث و اختر المحتوى
            Loaded += async (s, e) =>
            {
                await LoadProductsColumnsPreferenceAsync();
                try
                {
                    ProductsSearchBox?.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        try
                        {
                            ProductsSearchBox?.Focus();
                            Keyboard.Focus(ProductsSearchBox);
                            ProductsSearchBox?.SelectAll();
                        }
                        catch { }
                    }));
                }
                catch { }
            };
        }

        public ProductsWindow(MainViewModel vm) : this()
        {
            DataContext = vm;
        }

        public static void ShowSingle(MainViewModel vm = null)
        {
            // ✅ إذا كانت النافذة مفتوحة، فقط اعرضها
            if (_instance != null && _instance.IsLoaded)
            {
                _instance.Activate();
                _instance.Focus();
                return;
            }

            // إنشاء نافذة جديدة
            var mainVm = vm ?? (Application.Current?.MainWindow?.DataContext as MainViewModel) ?? new MainViewModel();
            _instance = new ProductsWindow(mainVm)
            {
                Owner = Application.Current?.MainWindow // ✅ ربط بالنافذة الرئيسية
            };

            // Apply modal behavior and prevent minimize (keep only Close)
            try { _instance.WindowStartupLocation = WindowStartupLocation.CenterOwner; } catch { }
            try { _instance.ShowInTaskbar = false; } catch { }
            try { _instance.ResizeMode = ResizeMode.NoResize; } catch { }
            try { _instance.WindowStyle = WindowStyle.SingleBorderWindow; } catch { }

            _instance.ShowDialog();
        }

        private void ShowEmbeddedPage(Page page, string title)
        {
            EmbeddedControlHost.Content = null;
            EmbeddedControlHost.Visibility = Visibility.Collapsed;

            EmbeddedPageHost.Navigate(page);
            EmbeddedPageHost.Visibility = Visibility.Visible;
            ProductsGrid.Visibility = Visibility.Collapsed;
            ContentAreaTitle.Text = title;

            // إظهار زر الرجوع
            if (FindName("BackToProductsButton") is Button btn)
                btn.Visibility = Visibility.Visible;
        }

        private void ShowEmbeddedProductEditor(Product? productToEdit = null)
        {
            EmbeddedPageHost.Content = null;
            EmbeddedPageHost.Visibility = Visibility.Collapsed;

            _isEditMode = productToEdit != null;

            var editorWindow = productToEdit != null
                ? new ProductEditorWindow(productToEdit) { IsEmbeddedMode = true }
                : new ProductEditorWindow { IsEmbeddedMode = true };

            editorWindow.SaveRequested += OnEmbeddedProductSaveRequested;
            editorWindow.CancelRequested += OnEmbeddedProductCancelRequested;

            if (editorWindow.Content is UIElement content)
            {
                editorWindow.Content = null;
                _embeddedProductEditor = editorWindow;

                // ✅ تأكيد أن المحرر المضمّن يأخذ DataContext المنتج المختار
                if (content is FrameworkElement fe)
                {
                    fe.DataContext = editorWindow.Product;
                    // Ensure width behavior is flexible when embedded
                    fe.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    fe.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                    fe.MinWidth = 600; // prevent extreme narrow layout that causes overlap
                }

                // Wrap content in a ScrollViewer to avoid layout overlap on small containers
                var sv = new System.Windows.Controls.ScrollViewer
                {
                    Content = content,
                    HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    Padding = new System.Windows.Thickness(0)
                };

                EmbeddedControlHost.Content = sv;
                EmbeddedControlHost.Visibility = Visibility.Visible;
                ProductsGrid.Visibility = Visibility.Collapsed;
                ContentAreaTitle.Text = _isEditMode ? "تعديل منتج" : "إضافة منتج";
            }
        }

        private async void OnEmbeddedProductSaveRequested(Product product)
        {
            try
            {
                bool isNew = !_isEditMode;

                if (_isEditMode)
                    await _productService.UpdateProductAsync(product);
                else
                    await _productService.AddProductAsync(product);

                if (DataContext is MainViewModel vm)
                    vm.SearchCommand?.Execute(null);

                try { App.SharedAppState?.NotifyProductsChanged(); } catch { }
                ShowProductsTable_Click(this, new RoutedEventArgs());

                // ✅ فقط المنتج الجديد ينتقل لأعلى الجدول
                if (isNew)
                    ScrollToProductTop(product);
            }
            catch (DuplicateProductException ex)
            {
                // نُبقي المحرر مفتوحاً ليصحح المستخدم البيانات
                MessageBox.Show(ex.Message, "تكرار منتج", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnEmbeddedProductCancelRequested()
        {
            ShowProductsTable_Click(this, new RoutedEventArgs());
        }

        private void OpenCategories_Click(object sender, RoutedEventArgs e)
        {
            ShowEmbeddedPage(new CategoriesPage(), "إدارة الفئات");
        }

        private void OpenUnits_Click(object sender, RoutedEventArgs e)
        {
            ShowEmbeddedPage(new UnitsPage(), "إدارة الوحدات");
        }

        private void ShowProductsTable_Click(object sender, RoutedEventArgs e)
        {
            EmbeddedPageHost.Content = null;
            EmbeddedPageHost.Visibility = Visibility.Collapsed;

            EmbeddedControlHost.Content = null;
            EmbeddedControlHost.Visibility = Visibility.Collapsed;

            if (_embeddedProductEditor != null)
            {
                _embeddedProductEditor.SaveRequested -= OnEmbeddedProductSaveRequested;
                _embeddedProductEditor.CancelRequested -= OnEmbeddedProductCancelRequested;
                _embeddedProductEditor = null;
            }

            _isEditMode = false;
            ProductsGrid.Visibility = Visibility.Visible;
            ContentAreaTitle.Text = "جدول المنتجات";

            // إخفاء زر الرجوع
            if (FindName("BackToProductsButton") is Button backBtn)
                backBtn.Visibility = Visibility.Collapsed;
        }

        private void OpenAddProductInPlace_Click(object sender, RoutedEventArgs e)
        {
            ShowEmbeddedProductEditor();
        }

        private void OpenEditProductInPlace_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Product product)
            {
                ShowEmbeddedProductEditor(product);
            }
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Live normalization for search box (barcode friendly)
        private void SearchBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            var normalized = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.FixBarcodeText(e.Text);
            if (normalized == e.Text) return;
            e.Handled = true;
            var tb = (TextBox)sender;
            var caret = tb.CaretIndex;
            if (tb.SelectionLength > 0)
                tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
            tb.Text = tb.Text.Insert(tb.CaretIndex, normalized);
            tb.CaretIndex = caret + normalized.Length;
        }

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.V && System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            {
                e.Handled = true;
                var tb = (TextBox)sender;
                var pasted = System.Windows.Clipboard.GetText() ?? string.Empty;
                var normalized = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(pasted);
                if (tb.SelectionLength > 0)
                    tb.Text = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength);
                tb.Text = tb.Text.Insert(tb.CaretIndex, normalized);
                tb.CaretIndex = tb.Text.Length;
                return;
            }

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                ((TextBox)sender).Clear();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                // On Enter, if barcode-like, select matching product and keep filter
                var tb = sender as TextBox;
                if (tb != null)
                {
                    var txt = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(tb.Text);
                    if (POS_BIGA.Infrastructure.Helpers.BarcodeHelper.IsCompleteScan(txt))
                    {
                        TrySelectProductByBarcode(txt);
                        e.Handled = true;
                    }
                }
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            var normalized = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(tb.Text);
            if (tb.Text != normalized) tb.Text = normalized;
            // If barcode-like input, try to apply filter and select matching product
            TrySelectProductByBarcode(tb.Text);
        }

        // ══ تعديل مباشر في الجدول ══

        private void EditableCell_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                    new Action(tb.SelectAll));
            }
        }

        private void NumericCell_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            var proposed = tb?.Text.Insert(tb.SelectionStart, e.Text) ?? e.Text;
            // يسمح بأرقام ونقطة عشرية واحدة فقط
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(proposed, @"^\d*\.?\d*$");
        }

        private async void ProductsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not Product product) return;

            // نترك الـ DataGrid يُطبّق التغيير على الـ binding أولاً
            await System.Threading.Tasks.Task.Yield();

            try
            {
                // تطبيع الباركود عند تعديله
                if (e.Column == ProductsBarcodeColumn)
                    product.Barcode = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(product.Barcode ?? string.Empty);

                // التحقق من صحة السعر
                if (e.Column == ProductsSalePriceColumn && product.SalePrice <= 0)
                {
                    MessageBox.Show("سعر البيع يجب أن يكون أكبر من صفر.", "قيمة غير صالحة", MessageBoxButton.OK, MessageBoxImage.Warning);
                    e.Cancel = true;
                    return;
                }

                await _productService.UpdateProductAsync(product);

                // إشعار بالتغيير
                try { App.SharedAppState?.NotifyProductsChanged(); } catch { }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"فشل حفظ التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var compact = e.NewSize.Width <= 1180;

                ProductsGrid.FontSize = compact ? 12 : 15;
                ProductsGrid.RowHeight = compact ? 62 : 80;
                ProductsGrid.ColumnHeaderHeight = compact ? 44 : 52;

                ProductsImageColumn.Width = new DataGridLength(compact ? 64 : 86);
                ProductsIdColumn.Width = new DataGridLength(compact ? 52 : 65);
                ProductsNameColumn.Width = compact
                    ? new DataGridLength(1.8, DataGridLengthUnitType.Star)
                    : new DataGridLength(1, DataGridLengthUnitType.Star);
                ProductsBarcodeColumn.Width = new DataGridLength(compact ? 110 : 130);
                ProductsCategoryColumn.Width = new DataGridLength(compact ? 96 : 120);
                ProductsQuantityColumn.Width = new DataGridLength(compact ? 84 : 100);
                ProductsUnitColumn.Width = new DataGridLength(compact ? 72 : 90);
                ProductsSalePriceColumn.Width = new DataGridLength(compact ? 100 : 120);
                ProductsPurchasePriceColumn.Width = new DataGridLength(compact ? 100 : 120);
                ProductsWholesaleColumn.Width = new DataGridLength(compact ? 96 : 110);
                ProductsInstallmentColumn.Width = new DataGridLength(compact ? 96 : 110);
                ProductsStockValueColumn.Width = new DataGridLength(compact ? 112 : 140);
                ProductsActionsColumn.Width = new DataGridLength(compact ? 160 : 180);
            }
            catch { }
        }

        private void ProductsColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.ContextMenu == null) return;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            SyncProductsColumnMenuChecks(button.ContextMenu);
            button.ContextMenu.IsOpen = true;
        }

        private void ProductsColumnVisibilityMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string columnKey) return;
            var column = GetProductsColumnByKey(columnKey);
            if (column == null) return;

            column.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;

            if (!ProductsGrid.Columns.Any(c => c.Visibility == Visibility.Visible))
            {
                column.Visibility = Visibility.Visible;
                menuItem.IsChecked = true;
            }

            _ = SaveProductsColumnsPreferenceAsync();
        }

        private void ResetProductsColumns_Click(object sender, RoutedEventArgs e)
        {
            foreach (var col in ProductsGrid.Columns)
                col.Visibility = Visibility.Visible;

            if (sender is MenuItem mi && mi.Parent is ContextMenu cm)
                SyncProductsColumnMenuChecks(cm);

            _ = SaveProductsColumnsPreferenceAsync();
        }

        private IEnumerable<(string Key, DataGridColumn Column)> GetProductsColumnEntries()
        {
            yield return (nameof(ProductsImageColumn), ProductsImageColumn);
            yield return (nameof(ProductsIdColumn), ProductsIdColumn);
            yield return (nameof(ProductsNameColumn), ProductsNameColumn);
            yield return (nameof(ProductsBarcodeColumn), ProductsBarcodeColumn);
            yield return (nameof(ProductsCategoryColumn), ProductsCategoryColumn);
            yield return (nameof(ProductsQuantityColumn), ProductsQuantityColumn);
            yield return (nameof(ProductsUnitColumn), ProductsUnitColumn);
            yield return (nameof(ProductsSalePriceColumn), ProductsSalePriceColumn);
            yield return (nameof(ProductsPurchasePriceColumn), ProductsPurchasePriceColumn);
            yield return (nameof(ProductsWholesaleColumn), ProductsWholesaleColumn);
            yield return (nameof(ProductsInstallmentColumn), ProductsInstallmentColumn);
            yield return (nameof(ProductsStockValueColumn), ProductsStockValueColumn);
            yield return (nameof(ProductsActionsColumn), ProductsActionsColumn);
        }

        private async System.Threading.Tasks.Task SaveProductsColumnsPreferenceAsync()
        {
            try
            {
                var visible = GetProductsColumnEntries()
                    .Where(x => x.Column.Visibility == Visibility.Visible)
                    .Select(x => x.Key)
                    .ToList();

                var json = JsonSerializer.Serialize(visible);
                await _appSettingService.SetValueAsync(ProductsColumnsSettingKey, json, "Visible columns for products grid");
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadProductsColumnsPreferenceAsync()
        {
            try
            {
                var setting = await _appSettingService.GetByKeyAsync(ProductsColumnsSettingKey);
                if (setting == null || string.IsNullOrWhiteSpace(setting.Value)) return;

                var visibleKeys = JsonSerializer.Deserialize<List<string>>(setting.Value) ?? new List<string>();
                var visibleSet = new HashSet<string>(visibleKeys);

                foreach (var entry in GetProductsColumnEntries())
                {
                    entry.Column.Visibility = visibleSet.Contains(entry.Key) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!ProductsGrid.Columns.Any(c => c.Visibility == Visibility.Visible))
                    ProductsNameColumn.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void SyncProductsColumnMenuChecks(ContextMenu menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is not MenuItem mi || mi.Tag is not string key || !mi.IsCheckable) continue;
                var col = GetProductsColumnByKey(key);
                if (col != null) mi.IsChecked = col.Visibility == Visibility.Visible;
            }
        }

        private DataGridColumn? GetProductsColumnByKey(string key)
        {
            return key switch
            {
                nameof(ProductsImageColumn) => ProductsImageColumn,
                nameof(ProductsIdColumn) => ProductsIdColumn,
                nameof(ProductsNameColumn) => ProductsNameColumn,
                nameof(ProductsBarcodeColumn) => ProductsBarcodeColumn,
                nameof(ProductsCategoryColumn) => ProductsCategoryColumn,
                nameof(ProductsQuantityColumn) => ProductsQuantityColumn,
                nameof(ProductsUnitColumn) => ProductsUnitColumn,
                nameof(ProductsSalePriceColumn) => ProductsSalePriceColumn,
                nameof(ProductsPurchasePriceColumn) => ProductsPurchasePriceColumn,
                nameof(ProductsWholesaleColumn) => ProductsWholesaleColumn,
                nameof(ProductsInstallmentColumn) => ProductsInstallmentColumn,
                nameof(ProductsStockValueColumn) => ProductsStockValueColumn,
                nameof(ProductsActionsColumn) => ProductsActionsColumn,
                _ => null
            };
        }

        private void TrySelectProductByBarcode(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var query = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(text);
            if (!POS_BIGA.Infrastructure.Helpers.BarcodeHelper.IsCompleteScan(query)) return;

            if (DataContext is POS_BIGA.ViewModels.MainViewModel vm)
            {
                var found = vm.Products.FirstOrDefault(p =>
                    string.Equals((p.Barcode ?? string.Empty).Trim(), query, StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    ProductsGrid.SelectedItem = found;
                    ProductsGrid.ScrollIntoView(found);
                    ProductsGrid.Focus();
                }
            }
        }

        /// <summary>
        /// ينقل المنتج لأعلى القائمة ويحدّده ويعرضه
        /// </summary>
        private void ScrollToProductTop(Product product)
        {
            if (product == null) return;

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    if (DataContext is not MainViewModel vm) return;

                    // البحث عن المنتج في القائمة (بالـ Id لأن المرجع قد يتغير بعد إعادة التحميل)
                    var found = vm.Products.FirstOrDefault(p => p.Id == product.Id);
                    if (found == null) return;

                    var idx = vm.Products.IndexOf(found);
                    if (idx > 0)
                    {
                        // نقله لأعلى القائمة
                        vm.Products.Move(idx, 0);
                    }

                    // تحديده وعرضه
                    ProductsGrid.SelectedItem = found;
                    ProductsGrid.ScrollIntoView(found);
                    ProductsGrid.Focus();
                }
                catch { }
            }));
        }
    }
}

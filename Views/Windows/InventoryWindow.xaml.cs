using System;
using System.Windows;
using POS_BIGA.ViewModels;
using POS_BIGA.Views.Windows;
using POS_BIGA.Models;
using System.Windows.Controls;
using POS_BIGA.Views.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using POS_BIGA.Services;
using System.Text.Json;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// نافذة إدارة المخزون - مع ربط كامل بقاعدة البيانات والمنتجات
    /// </summary>
    public partial class InventoryWindow : Window
    {
        private InventoryViewModel _viewModel;
        private readonly IAppSettingService _appSettingService = new AppSettingService();
        private const string InventoryColumnsSettingKey = "UI.InventoryGrid.VisibleColumns";
        private ProductEditorWindow? _embeddedProductEditor;
        private bool _isEditMode;

        public InventoryWindow()
        {
            InitializeComponent();

            // ✅ إنشاء ViewModel وربطه بالنافذة
            _viewModel = new InventoryViewModel();
            DataContext = _viewModel;

            // ✅ تحميل البيانات عند فتح النافذة
            Loaded += async (s, e) =>
            {
                await _viewModel.LoadInventoryAsync();
                await LoadInventoryColumnsPreferenceAsync();
                try
                {
                    InventorySearchBox?.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        try { InventorySearchBox?.Focus(); Keyboard.Focus(InventorySearchBox); InventorySearchBox?.SelectAll(); } catch { }
                    }));
                }
                catch { }
            };

            // ✅ تنظيف الموارد عند إغلاق النافذة
            Unloaded += (s, e) =>
            {
                _viewModel?.Dispose();
            };
        }

        // ✅ UI-only event (double-click to edit)
        private void DataGridRow_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenEditProductInPlace_Click(sender, e);
        }

        private void ShowEmbeddedProductEditor(Product? productToEdit = null)
        {
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

                if (content is FrameworkElement fe)
                    fe.DataContext = editorWindow.Product;

                EmbeddedEditorHost.Content = content;
                EmbeddedEditorHost.Visibility = Visibility.Visible;
                InventoryDataGrid.Visibility = Visibility.Collapsed;
                MainContentTitle.Text = _isEditMode ? "تعديل منتج" : "إضافة منتج";
            }
        }

        private async void OnEmbeddedProductSaveRequested(Product product)
        {
            try
            {
                if (_isEditMode)
                    await _viewModel.UpdateProductFromEditorAsync(product);
                else
                    await _viewModel.AddProductFromEditorAsync(product);

                ShowInventoryTable_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء حفظ المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnEmbeddedProductCancelRequested()
        {
            ShowInventoryTable_Click(this, new RoutedEventArgs());
        }

        private void OpenAddProductInPlace_Click(object sender, RoutedEventArgs e)
        {
            ShowEmbeddedProductEditor();
        }

        private async void OpenEditProductInPlace_Click(object sender, RoutedEventArgs e)
        {
            var selected = _viewModel?.SelectedItem;
            if (selected == null) return;

            var product = await _viewModel.GetProductForEditAsync(selected.ProductId);
            if (product == null) return;

            ShowEmbeddedProductEditor(product);
        }

        private void ShowInventoryTable_Click(object sender, RoutedEventArgs e)
        {
            EmbeddedEditorHost.Content = null;
            EmbeddedEditorHost.Visibility = Visibility.Collapsed;
            InventoryDataGrid.Visibility = Visibility.Visible;
            MainContentTitle.Text = "جدول المخزون";

            _ = _viewModel.ResetQuickFiltersAsync();

            if (_embeddedProductEditor != null)
            {
                _embeddedProductEditor.SaveRequested -= OnEmbeddedProductSaveRequested;
                _embeddedProductEditor.CancelRequested -= OnEmbeddedProductCancelRequested;
                _embeddedProductEditor = null;
            }

            _isEditMode = false;
        }

        private void ShowAdvancedStats_Click(object sender, RoutedEventArgs e)
        {
            if (_embeddedProductEditor != null)
            {
                _embeddedProductEditor.SaveRequested -= OnEmbeddedProductSaveRequested;
                _embeddedProductEditor.CancelRequested -= OnEmbeddedProductCancelRequested;
                _embeddedProductEditor = null;
            }

            _isEditMode = false;

            var statsView = new InventoryAdvancedStatsControl
            {
                DataContext = _viewModel
            };

            statsView.RequestShowTable += () =>
            {
                ShowInventoryTable_Click(this, new RoutedEventArgs());
            };

            EmbeddedEditorHost.Content = statsView;
            EmbeddedEditorHost.Visibility = Visibility.Visible;
            InventoryDataGrid.Visibility = Visibility.Collapsed;
            MainContentTitle.Text = "إحصائيات متقدمة للمخزون";
        }

        // ✅ إغلاق النافذة
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
                var tb = sender as TextBox;
                if (tb != null)
                {
                    var txt = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(tb.Text);
                    if (POS_BIGA.Infrastructure.Helpers.BarcodeHelper.IsCompleteScan(txt))
                    {
                        TrySelectInventoryByBarcode(txt);
                        e.Handled = true;
                    }
                }
            }
        }

        private void TrySelectInventoryByBarcode(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var query = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(text);
            if (!POS_BIGA.Infrastructure.Helpers.BarcodeHelper.IsCompleteScan(query)) return;

            // If ViewModel has PagedInventoryItems, try to find matching item
            if (DataContext is POS_BIGA.ViewModels.InventoryViewModel vm)
            {
                var found = vm.PagedInventoryItems.FirstOrDefault(i => string.Equals((i.Barcode ?? string.Empty).Trim(), query, StringComparison.OrdinalIgnoreCase));
                if (found != null)
                {
                    InventoryDataGrid.SelectedItem = found;
                    InventoryDataGrid.ScrollIntoView(found);
                    InventoryDataGrid.Focus();
                }
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            var normalized = POS_BIGA.Infrastructure.Helpers.BarcodeHelper.Normalize(tb.Text);
            if (tb.Text != normalized) tb.Text = normalized;
            TrySelectInventoryByBarcode(tb.Text);
        }

        private void InventorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void InventoryDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                var compact = e.NewSize.Width <= 1060;

                InventoryDataGrid.FontSize = compact ? 12 : 14;
                InventoryDataGrid.RowHeight = compact ? 60 : 72;

                InventoryIdColumn.Width = new DataGridLength(compact ? 52 : 65);
                InventoryNameColumn.Width = compact
                    ? new DataGridLength(1.8, DataGridLengthUnitType.Star)
                    : new DataGridLength(1, DataGridLengthUnitType.Star);
                InventoryBarcodeColumn.Width = new DataGridLength(compact ? 108 : 130);
                InventoryCategoryColumn.Width = new DataGridLength(compact ? 96 : 120);
                InventoryQuantityColumn.Width = new DataGridLength(compact ? 84 : 100);
                InventoryPriceColumn.Width = new DataGridLength(compact ? 100 : 120);
                InventoryWholesaleColumn.Width = new DataGridLength(compact ? 94 : 110);
                InventoryInstallmentColumn.Width = new DataGridLength(compact ? 94 : 110);
                InventoryStockValueColumn.Width = new DataGridLength(compact ? 112 : 140);
            }
            catch { }
        }

        private void InventoryColumnsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.ContextMenu == null) return;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Bottom;
            SyncInventoryColumnMenuChecks(button.ContextMenu);
            button.ContextMenu.IsOpen = true;
        }

        private void InventoryColumnVisibilityMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not string columnKey) return;
            var column = GetInventoryColumnByKey(columnKey);
            if (column == null) return;

            column.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;

            if (!InventoryDataGrid.Columns.Any(c => c.Visibility == Visibility.Visible))
            {
                column.Visibility = Visibility.Visible;
                menuItem.IsChecked = true;
            }

            _ = SaveInventoryColumnsPreferenceAsync();
        }

        private void ResetInventoryColumns_Click(object sender, RoutedEventArgs e)
        {
            foreach (var col in InventoryDataGrid.Columns)
                col.Visibility = Visibility.Visible;

            if (sender is MenuItem mi && mi.Parent is ContextMenu cm)
                SyncInventoryColumnMenuChecks(cm);

            _ = SaveInventoryColumnsPreferenceAsync();
        }

        private IEnumerable<(string Key, DataGridColumn Column)> GetInventoryColumnEntries()
        {
            yield return (nameof(InventoryIdColumn), InventoryIdColumn);
            yield return (nameof(InventoryNameColumn), InventoryNameColumn);
            yield return (nameof(InventoryBarcodeColumn), InventoryBarcodeColumn);
            yield return (nameof(InventoryCategoryColumn), InventoryCategoryColumn);
            yield return (nameof(InventoryQuantityColumn), InventoryQuantityColumn);
            yield return (nameof(InventoryPriceColumn), InventoryPriceColumn);
            yield return (nameof(InventoryWholesaleColumn), InventoryWholesaleColumn);
            yield return (nameof(InventoryInstallmentColumn), InventoryInstallmentColumn);
            yield return (nameof(InventoryStockValueColumn), InventoryStockValueColumn);
        }

        private async System.Threading.Tasks.Task SaveInventoryColumnsPreferenceAsync()
        {
            try
            {
                var visible = GetInventoryColumnEntries()
                    .Where(x => x.Column.Visibility == Visibility.Visible)
                    .Select(x => x.Key)
                    .ToList();

                var json = JsonSerializer.Serialize(visible);
                await _appSettingService.SetValueAsync(InventoryColumnsSettingKey, json, "Visible columns for inventory grid");
            }
            catch { }
        }

        private async System.Threading.Tasks.Task LoadInventoryColumnsPreferenceAsync()
        {
            try
            {
                var setting = await _appSettingService.GetByKeyAsync(InventoryColumnsSettingKey);
                if (setting == null || string.IsNullOrWhiteSpace(setting.Value)) return;

                var visibleKeys = JsonSerializer.Deserialize<List<string>>(setting.Value) ?? new List<string>();
                var visibleSet = new HashSet<string>(visibleKeys);

                foreach (var entry in GetInventoryColumnEntries())
                {
                    entry.Column.Visibility = visibleSet.Contains(entry.Key) ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!InventoryDataGrid.Columns.Any(c => c.Visibility == Visibility.Visible))
                    InventoryNameColumn.Visibility = Visibility.Visible;
            }
            catch { }
        }

        private void SyncInventoryColumnMenuChecks(ContextMenu menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is not MenuItem mi || mi.Tag is not string key || !mi.IsCheckable) continue;
                var col = GetInventoryColumnByKey(key);
                if (col != null) mi.IsChecked = col.Visibility == Visibility.Visible;
            }
        }

        private DataGridColumn? GetInventoryColumnByKey(string key)
        {
            return key switch
            {
                nameof(InventoryIdColumn) => InventoryIdColumn,
                nameof(InventoryNameColumn) => InventoryNameColumn,
                nameof(InventoryBarcodeColumn) => InventoryBarcodeColumn,
                nameof(InventoryCategoryColumn) => InventoryCategoryColumn,
                nameof(InventoryQuantityColumn) => InventoryQuantityColumn,
                nameof(InventoryPriceColumn) => InventoryPriceColumn,
                nameof(InventoryWholesaleColumn) => InventoryWholesaleColumn,
                nameof(InventoryInstallmentColumn) => InventoryInstallmentColumn,
                nameof(InventoryStockValueColumn) => InventoryStockValueColumn,
                _ => null
            };
        }
    }
}

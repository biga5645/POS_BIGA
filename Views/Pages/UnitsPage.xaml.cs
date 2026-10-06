using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using POS_BIGA.ViewModels;
using POS_BIGA.Models;
using System.Windows.Input;

namespace POS_BIGA.Views.Pages
{
    public partial class UnitsPage : Page
    {
        public UnitsPage()
        {
            InitializeComponent();
            DataContext = new UnitViewModel();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Unit selectedUnit)
            {
                var viewModel = DataContext as UnitViewModel;
                if (viewModel != null)
                {
                    // تحديد الوحدة المختارة
                    viewModel.SelectedUnit = selectedUnit;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ تم تحديد الوحدة: {selectedUnit.Name} (ID: {selectedUnit.Id})");
                }
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Unit selectedUnit)
            {
                var viewModel = DataContext as UnitViewModel;
                if (viewModel != null)
                {
                    // استدعاء أمر التعديل من ViewModel
                    viewModel.SelectForEditCommand?.Execute(selectedUnit);
                    
                    System.Diagnostics.Debug.WriteLine($"📝 فتح تعديل الوحدة: {selectedUnit.Name} بـ double-click");
                }
            }
        }

        // Checkbox save logic moved to MVVM command: ToggleAllowCustomPriceCommand
    }

    /// <summary>
    /// ✨ Converter لتحويل الاختصار إلى أيقونة ديناميكية
    /// </summary>
    public class UnitIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string abbreviation)
            {
                var lower = abbreviation.ToLower();

                return lower switch
                {
                    "kg" or "كجم" or "كيلو" or "كيلوجرام" => "⚖️",
                    "g" or "غرام" or "جرام" => "⚡",
                    "m" or "متر" => "📏",
                    "l" or "لتر" => "💧",
                    "piece" or "قطعة" => "🧊",
                    "box" or "علبة" => "📦",
                    "ton" or "طن" => "🏗️",
                    "cm" or "سم" => "📐",
                    "ml" or "مل" => "💊",
                    "dozen" or "دزينة" => "🎁",
                    _ => "📦"
                };
            }
            return "📦";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ✨ Converter لعرض Empty State
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter?.ToString() == "Inverse";
            bool isEmpty = value is int count && count == 0;

            if (inverse)
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;

            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System.Windows.Controls;
using System.Windows.Input;
using POS_BIGA.ViewModels;
using POS_BIGA.Models;
using POS_BIGA.Services;
using System.Windows;

namespace POS_BIGA.Views.Pages
{
    /// <summary>
    /// نفس منطق UnitsPage - التخطيط الأفقي مع double-click للتعديل
    /// </summary>
    public partial class CategoriesPage : Page
    {
        public CategoriesPage()
        {
            InitializeComponent();
            var appState = Application.Current.Resources["AppStateService"] as IAppStateService;
            var categoryService = new CategoryService(new POS_BIGA.Data.Repositories.CategoryRepository(), appState);
            DataContext = new CategoryViewModel(categoryService, appState);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Category selectedCategory)
            {
                var viewModel = DataContext as CategoryViewModel;
                if (viewModel != null)
                {
                    // تحديد الفئة المختارة
                    viewModel.SelectedCategory = selectedCategory;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ تم تحديد الفئة: {selectedCategory.Name} (ID: {selectedCategory.Id})");
                }
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Category selectedCategory)
            {
                var viewModel = DataContext as CategoryViewModel;
                if (viewModel != null)
                {
                    // استدعاء أمر التعديل من ViewModel
                    viewModel.SelectForEditCommand?.Execute(selectedCategory);
                    
                    System.Diagnostics.Debug.WriteLine($"📝 فتح تعديل الفئة: {selectedCategory.Name} بـ double-click");
                }
            }
        }
    }
}

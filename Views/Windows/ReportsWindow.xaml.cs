using System.Windows;
using POS_BIGA.ViewModels;
using System.Windows.Media;

namespace POS_BIGA.Views.Windows
{
    public partial class ReportsWindow : Window
    {
        private AdvancedReportsViewModel _vm;

        public ReportsWindow()
        {
            InitializeComponent();

            // Use AdvancedReportsViewModel
            _vm = new AdvancedReportsViewModel();
            DataContext = _vm;

            Loaded += ReportsWindow_Loaded;
        }

        private async void ReportsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyResponsiveLayout();
                System.Diagnostics.Debug.WriteLine("🚀 ReportsWindow Loaded - Starting data load...");
                await _vm.LoadReportsAsync();
                System.Diagnostics.Debug.WriteLine("✅ ReportsWindow data loaded successfully");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading reports: {ex.Message}");
                MessageBox.Show(
                    $"حدث خطأ أثناء تحميل التقارير:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ReportsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            try
            {
                var isSmall = ActualWidth <= 1366;

                var headerGrid = FindName("HeaderGrid") as FrameworkElement;
                if (headerGrid != null)
                    headerGrid.Margin = isSmall ? new Thickness(14, 10, 14, 10) : new Thickness(20, 12, 20, 12);

                var statsCardsContainer = FindName("StatsCardsContainer") as FrameworkElement;
                if (statsCardsContainer != null)
                {
                    var scale = isSmall ? 0.85 : 1.0;
                    statsCardsContainer.LayoutTransform = new ScaleTransform(scale, scale);
                }

                var salesStatsGrid = FindName("SalesStatsGrid") as FrameworkElement;
                if (salesStatsGrid != null)
                    salesStatsGrid.Margin = isSmall ? new Thickness(0) : new Thickness(2, 0, 2, 0);

                var creditStatsGrid = FindName("CreditStatsGrid") as FrameworkElement;
                if (creditStatsGrid != null)
                    creditStatsGrid.Margin = isSmall ? new Thickness(0, 6, 0, 0) : new Thickness(2, 8, 2, 0);
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

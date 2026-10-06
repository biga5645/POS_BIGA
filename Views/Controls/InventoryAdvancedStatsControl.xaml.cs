using System.Windows.Controls;
using POS_BIGA.ViewModels;

namespace POS_BIGA.Views.Controls
{
    public partial class InventoryAdvancedStatsControl : UserControl
    {
        public event System.Action? RequestShowTable;

        public InventoryAdvancedStatsControl()
        {
            InitializeComponent();
        }

        private async void GoodStock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryViewModel vm)
            {
                await vm.ApplyQuickStockFilterAsync("متوفر");
                RequestShowTable?.Invoke();
            }
        }

        private async void LowStock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryViewModel vm)
            {
                await vm.ApplyQuickStockFilterAsync("منخفض");
                RequestShowTable?.Invoke();
            }
        }

        private async void OutOfStock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InventoryViewModel vm)
            {
                await vm.ApplyQuickStockFilterAsync("منتهي");
                RequestShowTable?.Invoke();
            }
        }

        private void BackToTable_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RequestShowTable?.Invoke();
        }
    }
}

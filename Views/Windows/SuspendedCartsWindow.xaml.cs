using System.Windows;
using System.Collections.Generic;
using POS_BIGA.Models;

namespace POS_BIGA.Views.Windows
{
    public partial class SuspendedCartsWindow : Window
    {
        public string? SelectedCartId { get; private set; }

        public SuspendedCartsWindow(List<SuspendedCart> carts)
        {
            InitializeComponent();
            var items = new List<object>();
            foreach (var c in carts)
            {
                items.Add(new { Id = c.Id, DisplayLabel = $"سلة {c.Id.Substring(0, 6)} - {c.Items.Count} منتجات - {c.CreatedAt:HH:mm}" });
            }
            CartsList.ItemsSource = items;
            if (items.Count > 0) CartsList.SelectedIndex = 0;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var sel = CartsList.SelectedItem;
            if (sel == null) { DialogResult = false; Close(); return; }
            var idProp = sel.GetType().GetProperty("Id");
            SelectedCartId = idProp?.GetValue(sel)?.ToString();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; Close();
        }
    }
}

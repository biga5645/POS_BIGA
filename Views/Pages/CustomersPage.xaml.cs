using System;
using System.Windows;
using System.Windows.Controls;
using POS_BIGA.ViewModels;
using POS_BIGA.Services;
using POS_BIGA.Views.Windows;

namespace POS_BIGA.Views.Pages
{
    public partial class CustomersPage : Page
    {
        private CustomerViewModel ViewModel => DataContext as CustomerViewModel;

        public CustomersPage()
        {
            try
            {
                InitializeComponent();
                var appState = App.SharedAppState;
                var custService = new CustomerService(new POS_BIGA.Data.Repositories.CustomerRepository(), appState);
                var creditService = new CreditService(new POS_BIGA.Data.Repositories.CreditRepository(), appState, custService);
                var vm = new CustomerViewModel(custService, creditService, appState);
                
                vm.RequestAddCustomer += () =>
                {
                    var win = new CustomerEditorWindow();
                    win.Owner = Window.GetWindow(this);
                    if (win.ShowDialog() == true) vm.RefreshCommand?.Execute(null);
                };

                vm.RequestShowCustomerDetails += (customer) =>
                {
                    var detailsWindow = new CustomerDetailsWindow(customer);
                    detailsWindow.Owner = Window.GetWindow(this);
                    detailsWindow.ShowDialog();
                };

                DataContext = vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"??? ???:\n{ex.Message}", "???", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void CustomersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel?.SelectedCustomer != null)
                ViewModel.ViewCustomerDetailsCommand?.Execute(ViewModel.SelectedCustomer);
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.SelectedCustomer != null)
                ViewModel.ViewCustomerDetailsCommand?.Execute(ViewModel.SelectedCustomer);
        }
    }
}

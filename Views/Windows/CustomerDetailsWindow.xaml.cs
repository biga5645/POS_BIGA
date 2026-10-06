using System.Windows;
using POS_BIGA.Models;
using POS_BIGA.ViewModels;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// Pure MVVM - No code-behind logic
    /// All logic is in CustomerDetailsViewModel
    /// </summary>
    public partial class CustomerDetailsWindow : Window
    {
        public CustomerDetailsWindow(Customer customer)
        {
            InitializeComponent();
            DataContext = new CustomerDetailsViewModel(customer.Id);
        }
    }
}

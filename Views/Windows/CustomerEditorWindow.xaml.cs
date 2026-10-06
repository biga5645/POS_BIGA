using System.Windows;
using POS_BIGA.ViewModels;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// Pure MVVM - No code-behind logic
    /// All logic is in CustomerEditorViewModel
    /// </summary>
    public partial class CustomerEditorWindow : Window
    {
        public CustomerEditorWindow()
        {
            InitializeComponent();
            // DataContext is set in XAML
        }

        public CustomerEditorWindow(int customerId)
        {
            InitializeComponent();
            // Override DataContext with specific customer ID
            DataContext = new CustomerEditorViewModel(customerId);
        }
    }
}

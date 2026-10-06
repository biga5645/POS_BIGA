using System;
using System.Windows;
using System.Windows.Controls;
using POS_BIGA.ViewModels;

namespace POS_BIGA.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingsPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
                vm.Users.FormPassword = pb.Password;
        }
    }
}
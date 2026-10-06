using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using POS_BIGA.Data.Repositories;
using POS_BIGA.Services;
using POS_BIGA.ViewModels;

namespace POS_BIGA.Views.Windows
{
    public partial class LoginWindow : Window
    {
        public bool LoginSucceeded { get; private set; } = false;
        private LoginViewModel _vm;

        public LoginWindow()
        {
            InitializeComponent();

            _vm = new LoginViewModel(new UserService(new UserRepository()));
            _vm.LoginSuccessful += user =>
            {
                LoginSucceeded = true;
                Dispatcher.Invoke(() =>
                {
                    App.OpenMainWindow();
                    Close();
                });
            };

            DataContext = _vm;

            Loaded += (s, e) =>
            {
                // ? ??? ??? ????? PIN ???????
                LoginTabControl.SelectedIndex = 0;
                // ??? ???? ???????? ??? ????? ??????? ?????
                this.KeyDown += Window_KeyDown;
            };
        }

        // =====================================================
        // ?? ??? ???? ???????? ??????? ?????? PIN
        // =====================================================
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // ? PIN ???? ??????? ????? (index 0)
            if (LoginTabControl.SelectedIndex != 0) return;

            string? digit = e.Key switch
            {
                Key.D0 or Key.NumPad0 => "0",
                Key.D1 or Key.NumPad1 => "1",
                Key.D2 or Key.NumPad2 => "2",
                Key.D3 or Key.NumPad3 => "3",
                Key.D4 or Key.NumPad4 => "4",
                Key.D5 or Key.NumPad5 => "5",
                Key.D6 or Key.NumPad6 => "6",
                Key.D7 or Key.NumPad7 => "7",
                Key.D8 or Key.NumPad8 => "8",
                Key.D9 or Key.NumPad9 => "9",
                _ => null
            };

            if (digit != null)
            {
                _vm.AppendPinCommand.Execute(digit);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                _vm.ClearPinCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                _vm.PinInput = string.Empty;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && _vm.LoginPinCommand.CanExecute(null))
            {
                _vm.LoginPinCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            LoginSucceeded = false;
            Application.Current.Shutdown();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                _vm.Password = pb.Password;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _vm.LoginCommand.CanExecute(null))
                _vm.LoginCommand.Execute(null);
        }
    }
}

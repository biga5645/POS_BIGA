using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace POS_BIGA.Behaviors
{
    /// <summary>
    /// Attached behavior: registers to the Application-level GotKeyboardFocus event once
    /// so every TextBox in the entire app selects all text when focused (click or Tab).
    /// Attach once in App.xaml.cs via TextBoxSelectAllBehavior.Register().
    /// </summary>
    public static class TextBoxSelectAllBehavior
    {
        private static bool _registered;

        /// <summary>
        /// Call once from App.OnStartup to activate SelectAll for every TextBox app-wide.
        /// </summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(OnTextBoxGotFocus),
                handledEventsToo: true);

            EventManager.RegisterClassHandler(
                typeof(TextBox),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnTextBoxPreviewMouseDown),
                handledEventsToo: false);
        }

        // ??? GotKeyboardFocus ??????????????????????????????????????????????????????
        private static void OnTextBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox tb || tb.IsReadOnly) return;

            // BeginInvoke ?? Input priority ???? ????? SelectAll ??? ?????? routing
            tb.Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(tb.SelectAll));
        }

        // ??? PreviewMouseLeftButtonDown ????????????????????????????????????????????
        // ??? ????? ??????? ??? ????? ??????? ??? ???? ??? ??????
        private static void OnTextBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.IsReadOnly) return;

            // ??? ?? ??? ??????? ?????? ???? ??? TextBox ???? ??????? ????? ?????? ?????????
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }
    }
}

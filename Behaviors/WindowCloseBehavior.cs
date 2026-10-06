using System.Windows;

namespace POS_BIGA.Behaviors
{
    /// <summary>
    /// Attached behavior to close window from ViewModel
    /// Usage: behaviors:WindowCloseBehavior.DialogResult="{Binding DialogResult}"
    /// </summary>
    public static class WindowCloseBehavior
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(WindowCloseBehavior),
                new PropertyMetadata(null, OnDialogResultChanged));

        public static void SetDialogResult(Window target, bool? value)
        {
            target.SetValue(DialogResultProperty, value);
        }

        public static bool? GetDialogResult(Window target)
        {
            return (bool?)target.GetValue(DialogResultProperty);
        }

        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && e.NewValue is bool result)
            {
                window.DialogResult = result;
                window.Close();
            }
        }
    }
}

using System.Windows;

namespace POS_BIGA.Resources.Icons
{
    public static class TabItemIcon
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(object),
            typeof(TabItemIcon),
            new PropertyMetadata(null));

        public static void SetIcon(DependencyObject element, object value) => element.SetValue(IconProperty, value);
        public static object GetIcon(DependencyObject element) => element.GetValue(IconProperty);
    }
}

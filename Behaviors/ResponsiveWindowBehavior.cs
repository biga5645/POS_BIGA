using System;
using System.Windows;
using System.Windows.Media;

namespace POS_BIGA.Behaviors
{
    public static class ResponsiveWindowBehavior
    {
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

        public static readonly DependencyProperty DesignWidthProperty = DependencyProperty.RegisterAttached(
            "DesignWidth",
            typeof(double),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(1600d));

        public static void SetDesignWidth(DependencyObject element, double value) => element.SetValue(DesignWidthProperty, value);
        public static double GetDesignWidth(DependencyObject element) => (double)element.GetValue(DesignWidthProperty);

        public static readonly DependencyProperty DesignHeightProperty = DependencyProperty.RegisterAttached(
            "DesignHeight",
            typeof(double),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(900d));

        public static void SetDesignHeight(DependencyObject element, double value) => element.SetValue(DesignHeightProperty, value);
        public static double GetDesignHeight(DependencyObject element) => (double)element.GetValue(DesignHeightProperty);

        public static readonly DependencyProperty MinScaleProperty = DependencyProperty.RegisterAttached(
            "MinScale",
            typeof(double),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(0.75d));

        public static void SetMinScale(DependencyObject element, double value) => element.SetValue(MinScaleProperty, value);
        public static double GetMinScale(DependencyObject element) => (double)element.GetValue(MinScaleProperty);

        public static readonly DependencyProperty MaxScaleProperty = DependencyProperty.RegisterAttached(
            "MaxScale",
            typeof(double),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(1.25d));

        public static void SetMaxScale(DependencyObject element, double value) => element.SetValue(MaxScaleProperty, value);
        public static double GetMaxScale(DependencyObject element) => (double)element.GetValue(MaxScaleProperty);

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.RegisterAttached(
            "Scale",
            typeof(double),
            typeof(ResponsiveWindowBehavior),
            new PropertyMetadata(1d));

        public static void SetScale(DependencyObject element, double value) => element.SetValue(ScaleProperty, value);
        public static double GetScale(DependencyObject element) => (double)element.GetValue(ScaleProperty);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
                return;

            if ((bool)e.NewValue)
            {
                window.Loaded += Window_Loaded;
                window.SizeChanged += Window_SizeChanged;
                window.StateChanged += Window_StateChanged;
            }
            else
            {
                window.Loaded -= Window_Loaded;
                window.SizeChanged -= Window_SizeChanged;
                window.StateChanged -= Window_StateChanged;
            }
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
                UpdateScale(window);
        }

        private static void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Window window)
                UpdateScale(window);
        }

        private static void Window_StateChanged(object sender, EventArgs e)
        {
            if (sender is Window window)
                UpdateScale(window);
        }

        private static void UpdateScale(Window window)
        {
            var designWidth = GetDesignWidth(window);
            var designHeight = GetDesignHeight(window);
            var minScale = GetMinScale(window);
            var maxScale = GetMaxScale(window);

            var width = Math.Max(1d, window.ActualWidth > 0 ? window.ActualWidth : window.Width);
            var height = Math.Max(1d, window.ActualHeight > 0 ? window.ActualHeight : window.Height);

            var sx = width / Math.Max(1d, designWidth);
            var sy = height / Math.Max(1d, designHeight);
            var scale = Math.Min(sx, sy);
            scale = Math.Max(minScale, Math.Min(maxScale, scale));

            SetScale(window, scale);
        }
    }
}

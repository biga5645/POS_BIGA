using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace POS_BIGA.ViewModels
{
    public class QuickCashViewModel : INotifyPropertyChanged
    {
        private readonly object _parent;

        public QuickCashViewModel(object parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            // try to proxy parent's AddCashCommand and ClearCashCommand via reflection
            AddCashCommand = TryGetCommandFromParent("AddCashCommand");

            QuickAddCommand = new RelayCommand<string>(param =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(param))
                    {
                        if (AddCashCommand != null && AddCashCommand.CanExecute(param)) AddCashCommand.Execute(param);
                    }
                }
                catch { }
            });

            ClearCashCommand = new RelayCommand(() =>
            {
                try
                {
                    var cmd = TryGetCommandFromParent("ClearCashCommand");
                    if (cmd != null && cmd.CanExecute(null)) cmd.Execute(null);
                }
                catch { }
            });

            CloseCommand = new RelayCommand(() =>
            {
                try
                {
                    var svc = Application.Current?.Resources["WindowService"];
                    if (svc != null)
                    {
                        var m = svc.GetType().GetMethod("CloseQuickCash");
                        m?.Invoke(svc, null);
                    }
                }
                catch { }
            });

            if (_parent is INotifyPropertyChanged ipc) ipc.PropertyChanged += Parent_PropertyChanged;

            // load images: try parent's image props first
            Cash0_5Image = TryGetImageFromParent("Cash0_5Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_0_5.png"));
            Cash1Image = TryGetImageFromParent("Cash1Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_1.png"));
            Cash2Image = TryGetImageFromParent("Cash2Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_2.png"));
            Cash5Image = TryGetImageFromParent("Cash5Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_5.png"));
            Cash10Image = TryGetImageFromParent("Cash10Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_10.png"));
            Cash20Image = TryGetImageFromParent("Cash20Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_20.png"));
            Cash50Image = TryGetImageFromParent("Cash50Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_50.png"));
            Cash100Image = TryGetImageFromParent("Cash100Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_100.png"));
            Cash200Image = TryGetImageFromParent("Cash200Image") ?? LoadBitmapSafeWithAlternatives(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "cash_200.png"));
        }

        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                // forward common properties if parent exposes them
                var name = e.PropertyName;
                if (string.IsNullOrEmpty(name)) return;

                if (name == "CashTendered" || name == "ChangeAmount")
                {
                    OnPropertyChanged(name);
                }

                // image updates
                if (name == "Cash0_5Image") { Cash0_5Image = TryGetImageFromParent("Cash0_5Image") ?? Cash0_5Image; }
                if (name == "Cash1Image") { Cash1Image = TryGetImageFromParent("Cash1Image") ?? Cash1Image; }
                if (name == "Cash2Image") { Cash2Image = TryGetImageFromParent("Cash2Image") ?? Cash2Image; }
                if (name == "Cash5Image") { Cash5Image = TryGetImageFromParent("Cash5Image") ?? Cash5Image; }
                if (name == "Cash10Image") { Cash10Image = TryGetImageFromParent("Cash10Image") ?? Cash10Image; }
                if (name == "Cash20Image") { Cash20Image = TryGetImageFromParent("Cash20Image") ?? Cash20Image; }
                if (name == "Cash50Image") { Cash50Image = TryGetImageFromParent("Cash50Image") ?? Cash50Image; }
                if (name == "Cash100Image") { Cash100Image = TryGetImageFromParent("Cash100Image") ?? Cash100Image; }
                if (name == "Cash200Image") { Cash200Image = TryGetImageFromParent("Cash200Image") ?? Cash200Image; }

                OnPropertyChanged(name);
            }
            catch { }
        }

        private ICommand TryGetCommandFromParent(string propName)
        {
            try
            {
                var p = _parent.GetType().GetProperty(propName);
                if (p != null)
                {
                    var val = p.GetValue(_parent);
                    if (val is ICommand cmd) return cmd;
                }
            }
            catch { }
            return null;
        }

        private ImageSource TryGetImageFromParent(string propName)
        {
            try
            {
                var p = _parent.GetType().GetProperty(propName);
                if (p != null)
                {
                    var val = p.GetValue(_parent);
                    if (val is ImageSource img) return img;
                }
            }
            catch { }
            return null;
        }

        // Try multiple filename variants (underscore, comma, dot, dash) to handle locales like 'cash_0,5.png'
        private ImageSource LoadBitmapSafeWithAlternatives(string baseFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(baseFilePath)) return null;
                var dir = Path.GetDirectoryName(baseFilePath) ?? string.Empty;
                var filename = Path.GetFileName(baseFilePath);
                var variants = new List<string> { filename };
                variants.Add(filename.Replace("_", ","));
                variants.Add(filename.Replace("_", "."));
                variants.Add(filename.Replace("_", "-"));

                foreach (var v in variants.Distinct())
                {
                    var p = Path.Combine(dir, v);
                    var img = LoadBitmapSafe(p);
                    if (img != null) return img;
                }
            }
            catch { }
            return null;
        }

        public ICommand AddCashCommand { get; }
        public ICommand QuickAddCommand { get; }
        public ICommand ClearCashCommand { get; }
        public ICommand CloseCommand { get; }

        public decimal CashTendered
        {
            get
            {
                try
                {
                    var p = _parent.GetType().GetProperty("CashTendered");
                    if (p != null) return Convert.ToDecimal(p.GetValue(_parent));
                }
                catch { }
                return 0m;
            }
        }

        public decimal ChangeAmount
        {
            get
            {
                try
                {
                    var p = _parent.GetType().GetProperty("ChangeAmount");
                    if (p != null) return Convert.ToDecimal(p.GetValue(_parent));
                }
                catch { }
                return 0m;
            }
        }

        private ImageSource _cash0_5Image;
        public ImageSource Cash0_5Image { get => _cash0_5Image; private set { _cash0_5Image = value; OnPropertyChanged(nameof(Cash0_5Image)); } }
        private ImageSource _cash1Image;
        public ImageSource Cash1Image { get => _cash1Image; private set { _cash1Image = value; OnPropertyChanged(nameof(Cash1Image)); } }
        private ImageSource _cash2Image;
        public ImageSource Cash2Image { get => _cash2Image; private set { _cash2Image = value; OnPropertyChanged(nameof(Cash2Image)); } }
        private ImageSource _cash5Image;
        public ImageSource Cash5Image { get => _cash5Image; private set { _cash5Image = value; OnPropertyChanged(nameof(Cash5Image)); } }
        private ImageSource _cash10Image;
        public ImageSource Cash10Image { get => _cash10Image; private set { _cash10Image = value; OnPropertyChanged(nameof(Cash10Image)); } }
        private ImageSource _cash20Image;
        public ImageSource Cash20Image { get => _cash20Image; private set { _cash20Image = value; OnPropertyChanged(nameof(Cash20Image)); } }
        private ImageSource _cash50Image;
        public ImageSource Cash50Image { get => _cash50Image; private set { _cash50Image = value; OnPropertyChanged(nameof(Cash50Image)); } }
        private ImageSource _cash100Image;
        public ImageSource Cash100Image { get => _cash100Image; private set { _cash100Image = value; OnPropertyChanged(nameof(Cash100Image)); } }
        private ImageSource _cash200Image;
        public ImageSource Cash200Image { get => _cash200Image; private set { _cash200Image = value; OnPropertyChanged(nameof(Cash200Image)); } }

        private ImageSource LoadBitmapSafe(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

                using (var bmp = new Bitmap(filePath))
                {
                    var hBitmap = bmp.GetHbitmap();
                    try
                    {
                        var img = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
                        img.Freeze();
                        return img;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        // Minimal RelayCommand implementations so ViewModel doesn't depend on external helpers
        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public RelayCommand(Action execute, Func<bool> canExecute = null) { _execute = execute; _canExecute = canExecute; }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
            public void Execute(object parameter) => _execute();
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;
            public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null) { _execute = execute; _canExecute = canExecute; }
            public event EventHandler CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
            public bool CanExecute(object parameter) { if (_canExecute == null) return true; if (parameter == null) return _canExecute(default); return _canExecute((T)parameter); }
            public void Execute(object parameter) { _execute((T)Convert.ChangeType(parameter, typeof(T))); }
        }
    }
}

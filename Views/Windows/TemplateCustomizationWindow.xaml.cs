using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Infrastructure.Printing.Views;
using POS_BIGA.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// نافذة تخصيص قالب الفاتورة
    /// </summary>
    public partial class TemplateCustomizationWindow : Window
    {
        private PaperSize _selectedPaperSize = PaperSize.A4;
        private string _selectedTemplate = "Professional"; // Professional or Simple
        private int _thermalWidth = 80; // 58 or 80

        public PaperSize SelectedPaperSize => _selectedPaperSize;
        public string SelectedTemplate => _selectedTemplate;
        public int ThermalWidth => _thermalWidth;
        public int DecimalPlaces => DecimalPlacesComboBox.SelectedIndex;
        public bool ShowLogo => ShowLogoCheckBox.IsChecked == true;
        public bool ShowBarcode => ShowBarcodeCheckBox.IsChecked == true;
        public bool ColorPrint => ColorPrintCheckBox.IsChecked == true;
        public bool ShowCashier => ShowCashierCheckBox.IsChecked == true;

        // Thermal settings
        public int FontSize => FontSizeComboBox?.SelectedIndex == 0 ? 10 : FontSizeComboBox?.SelectedIndex == 2 ? 14 : 12;
        public int Density => (int)(DensitySlider?.Value ?? 2);
        public bool ShowThermalHeader => ShowThermalHeaderCheckBox?.IsChecked == true;
        public bool ShowThermalLines => ShowThermalLinesCheckBox?.IsChecked == true;
        public bool ShowThermalTotals => ShowThermalTotalsCheckBox?.IsChecked == true;
        public bool CutAfterPrint => CutAfterPrintCheckBox?.IsChecked == true;

        public TemplateCustomizationWindow(string currentTemplate = "Professional")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎨 TemplateCustomizationWindow: بدء Constructor...");
                System.Diagnostics.Debug.WriteLine($"   القالب المستلم: {currentTemplate}");

                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ TemplateCustomizationWindow: تم InitializeComponent");

                // تطبيق القالب المختار
                _selectedTemplate = currentTemplate ?? "Professional";

                LoadCurrentSettings();
                System.Diagnostics.Debug.WriteLine("✅ TemplateCustomizationWindow: تم LoadCurrentSettings");

                System.Diagnostics.Debug.WriteLine("🎉 TemplateCustomizationWindow: Constructor مكتمل بنجاح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TemplateCustomizationWindow Constructor Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// تحميل الإعدادات الحالية
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📋 LoadCurrentSettings: بدء...");

                var settings = PrinterService.Instance.LoadSettings();
                System.Diagnostics.Debug.WriteLine($"   Paper Size: {settings.PaperSize}");
                System.Diagnostics.Debug.WriteLine($"   Decimal Places: {settings.DecimalPlaces}");
                System.Diagnostics.Debug.WriteLine($"   Template: {settings.TemplateName}");
                System.Diagnostics.Debug.WriteLine($"   Selected Template: {_selectedTemplate}");

                // تطبيق نوع القالب المختار
                if (_selectedTemplate == "Simple")
                {
                    Dispatcher.Invoke(() => 
                    {
                        Simple_Click(null, null);

                        // تطبيق عرض الورق الحراري
                        if (settings.ReceiptWidthMm == 58)
                        {
                            Width58_Click(null, null);
                        }
                        else
                        {
                            Width80_Click(null, null);
                        }
                    });
                    System.Diagnostics.Debug.WriteLine("   ✅ تم تطبيق Simple Template");
                }
                else // Professional
                {
                    Dispatcher.Invoke(() => Professional_Click(null, null));

                    // تطبيق حجم الورقة
                    if (settings.PaperSize == PaperSize.A5)
                    {
                        _selectedPaperSize = PaperSize.A5;
                        Dispatcher.Invoke(() => A5_Click(null, null));
                        System.Diagnostics.Debug.WriteLine("   ✅ تم تطبيق A5");
                    }
                    else
                    {
                        _selectedPaperSize = PaperSize.A4;
                        Dispatcher.Invoke(() => A4_Click(null, null));
                        System.Diagnostics.Debug.WriteLine("   ✅ تم تطبيق A4");
                    }

                    System.Diagnostics.Debug.WriteLine("   ✅ تم تطبيق Professional Template");
                }

                // تطبيق الأرقام العشرية
                Dispatcher.Invoke(() => 
                {
                    DecimalPlacesComboBox.SelectedIndex = settings.DecimalPlaces;
                });
                System.Diagnostics.Debug.WriteLine($"   ✅ تم تطبيق DecimalPlaces: {settings.DecimalPlaces}");

                StatusText.Text = "تم تحميل الإعدادات الحالية";
                System.Diagnostics.Debug.WriteLine("✅ LoadCurrentSettings: مكتمل بنجاح");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadCurrentSettings Error: {ex.Message}");
                StatusText.Text = $"خطأ في التحميل: {ex.Message}";

                // تطبيق الافتراضيات حسب القالب المختار
                _thermalWidth = 80;

                try
                {
                    Dispatcher.Invoke(() => 
                    {
                        if (_selectedTemplate == "Simple")
                        {
                            Simple_Click(null, null);
                            Width80_Click(null, null);
                        }
                        else
                        {
                            Professional_Click(null, null);
                            A4_Click(null, null);
                        }
                        DecimalPlacesComboBox.SelectedIndex = 2;
                    });
                    System.Diagnostics.Debug.WriteLine("⚠️ تم تطبيق الإعدادات الافتراضية");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ خطأ في تطبيق الافتراضيات: {ex2.Message}");
                }
            }
        }

        /// <summary>
        /// اختيار A4
        /// </summary>
        private void A4_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedPaperSize = PaperSize.A4;

            // تحديث المظهر
            A4Border.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            A4Border.BorderThickness = new Thickness(3);
            A4Border.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            A5Border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            A5Border.BorderThickness = new Thickness(2);
            A5Border.Background = Brushes.Transparent;

            StatusText.Text = "تم اختيار حجم A4 (210 × 297 مم)";
        }

        /// <summary>
        /// اختيار A5
        /// </summary>
        private void A5_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedPaperSize = PaperSize.A5;

            // تحديث المظهر
            A5Border.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            A5Border.BorderThickness = new Thickness(3);
            A5Border.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            A4Border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            A4Border.BorderThickness = new Thickness(2);
            A4Border.Background = Brushes.Transparent;

            StatusText.Text = "تم اختيار حجم A5 (148 × 210 مم)";
        }

        /// <summary>
        /// اختيار Professional Template
        /// </summary>
        private void Professional_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedTemplate = "Professional";

            // تحديث المظهر
            ProfessionalBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            ProfessionalBorder.BorderThickness = new Thickness(3);
            ProfessionalBorder.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            SimpleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            SimpleBorder.BorderThickness = new Thickness(2);
            SimpleBorder.Background = Brushes.Transparent;

            // إظهار/إخفاء الأقسام
            if (TemplateTypePanel != null) TemplateTypePanel.Visibility = Visibility.Collapsed;
            if (PaperSizePanel != null) PaperSizePanel.Visibility = Visibility.Visible;
            if (ThermalSettingsPanel != null) ThermalSettingsPanel.Visibility = Visibility.Collapsed;

            StatusText.Text = "قالب Professional - اختر حجم الورقة A4 أو A5";
        }

        /// <summary>
        /// اختيار Simple Template
        /// </summary>
        private void Simple_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedTemplate = "Simple";

            // تحديث المظهر
            SimpleBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            SimpleBorder.BorderThickness = new Thickness(3);
            SimpleBorder.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            ProfessionalBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            ProfessionalBorder.BorderThickness = new Thickness(2);
            ProfessionalBorder.Background = Brushes.Transparent;

            // إظهار/إخفاء الأقسام
            if (TemplateTypePanel != null) TemplateTypePanel.Visibility = Visibility.Collapsed;
            if (PaperSizePanel != null) PaperSizePanel.Visibility = Visibility.Collapsed;
            if (ThermalSettingsPanel != null) ThermalSettingsPanel.Visibility = Visibility.Visible;

            StatusText.Text = "قالب Simple - اختر إعدادات الطابعة الحرارية";
        }

        /// <summary>
        /// اختيار عرض 58mm
        /// </summary>
        private void Width58_Click(object sender, MouseButtonEventArgs e)
        {
            _thermalWidth = 58;

            Width58Border.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            Width58Border.BorderThickness = new Thickness(3);
            Width58Border.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            Width80Border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            Width80Border.BorderThickness = new Thickness(2);
            Width80Border.Background = Brushes.Transparent;

            StatusText.Text = "تم اختيار عرض 58 مم";
        }

        /// <summary>
        /// اختيار عرض 80mm
        /// </summary>
        private void Width80_Click(object sender, MouseButtonEventArgs e)
        {
            _thermalWidth = 80;

            Width80Border.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));
            Width80Border.BorderThickness = new Thickness(3);
            Width80Border.Background = new SolidColorBrush(Color.FromArgb(20, 99, 102, 241));

            Width58Border.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            Width58Border.BorderThickness = new Thickness(2);
            Width58Border.Background = Brushes.Transparent;

            StatusText.Text = "تم اختيار عرض 80 مم";
        }

        /// <summary>
        /// معاينة مع الإعدادات الحالية
        /// </summary>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "جاري إنشاء المعاينة...";

                var testSale = CreateTestSale();
                var settings = PrinterService.Instance.LoadSettings();

                if (_selectedTemplate == "Simple")
                {
                    settings.TemplateName = "Simple";
                    settings.ReceiptWidthMm = _thermalWidth;
                }
                else
                {
                    settings.TemplateName = "Professional A4/A5";
                    settings.PaperSize = _selectedPaperSize;
                }

                settings.DecimalPlaces = DecimalPlaces;

                Window previewWindow = _selectedTemplate == "Simple"
                    ? new SimpleTemplatePreviewWindow(testSale, settings)
                    : new ProfessionalTemplatePreviewWindow(testSale, settings);

                previewWindow.Owner = this;
                previewWindow.ShowDialog();

                StatusText.Text = "تم فتح المعاينة";
            }
            catch (Exception ex)
            {
                StatusText.Text = "فشلت المعاينة";
                MessageBox.Show(
                    $"خطأ في المعاينة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// حفظ الإعدادات وتطبيقها على القالب
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "جاري الحفظ...";
                SaveButton.IsEnabled = false;

                // تحميل الإعدادات الحالية
                var settings = PrinterService.Instance.LoadSettings();

                // تطبيق الإعدادات حسب القالب
                if (_selectedTemplate == "Simple")
                {
                    settings.TemplateName = "Simple";
                    settings.ReceiptWidthMm = _thermalWidth;
                    settings.ThermalFontSize = FontSize;
                    settings.ThermalDensity = Density;
                    settings.ShowThermalHeader = ShowThermalHeader;
                    settings.ShowThermalLines = ShowThermalLines;
                    settings.ShowThermalTotals = ShowThermalTotals;
                    settings.CutAfterPrint = CutAfterPrint;

                    System.Diagnostics.Debug.WriteLine($"💾 حفظ إعدادات Simple:");
                    System.Diagnostics.Debug.WriteLine($"   Width: {_thermalWidth}mm");
                    System.Diagnostics.Debug.WriteLine($"   Font Size: {FontSize}pt");
                    System.Diagnostics.Debug.WriteLine($"   Density: {Density}");
                    System.Diagnostics.Debug.WriteLine($"   Show Header: {ShowThermalHeader}");
                    System.Diagnostics.Debug.WriteLine($"   Show Lines: {ShowThermalLines}");
                    System.Diagnostics.Debug.WriteLine($"   Cut After Print: {CutAfterPrint}");
                }
                else // Professional
                {
                    settings.TemplateName = "Professional A4/A5";
                    settings.PaperSize = _selectedPaperSize;

                    System.Diagnostics.Debug.WriteLine($"💾 حفظ إعدادات Professional:");
                    System.Diagnostics.Debug.WriteLine($"   Paper Size: {_selectedPaperSize}");
                }

                settings.DecimalPlaces = DecimalPlaces;
                settings.ShowLogo = ShowLogo;
                settings.ShowCashier = ShowCashier;

                System.Diagnostics.Debug.WriteLine($"   Decimal Places: {DecimalPlaces}");
                System.Diagnostics.Debug.WriteLine($"   Show Logo: {ShowLogo}");
                System.Diagnostics.Debug.WriteLine($"   Show Cashier: {ShowCashier}");

                // حفظ الإعدادات
                PrinterService.Instance.SaveSettings(settings);

                DialogResult = true;
                StatusText.Text = "✅ تم الحفظ والتطبيق بنجاح";

                System.Diagnostics.Debug.WriteLine("✅ SaveButton_Click: تم الحفظ والتطبيق بنجاح");

                MessageBox.Show(
                    $"✅ تم حفظ وتطبيق الإعدادات بنجاح!\n\n" +
                    $"القالب: {_selectedTemplate}\n" +
                    (_selectedTemplate == "Simple" 
                        ? $"عرض الورق: {_thermalWidth}mm\n"
                        : $"حجم الورقة: {_selectedPaperSize}\n") +
                    $"الأرقام العشرية: {DecimalPlaces}\n\n" +
                    $"سيتم استخدام هذه الإعدادات في الطباعة القادمة.",
                    "تم بنجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SaveButton_Click Error: {ex.Message}");

                SaveButton.IsEnabled = true;
                StatusText.Text = "❌ فشل الحفظ";

                MessageBox.Show(
                    $"خطأ في الحفظ:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// إعادة تعيين إلى الإعدادات الافتراضية
        /// </summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "هل أنت متأكد من إعادة تعيين جميع الإعدادات إلى الافتراضي؟",
                    "تأكيد إعادة التعيين",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    // إعادة تعيين حسب القالب
                    if (_selectedTemplate == "Simple")
                    {
                        Width80_Click(null, null);
                        FontSizeComboBox.SelectedIndex = 1; // 12pt
                        DensitySlider.Value = 2;
                        ShowThermalHeaderCheckBox.IsChecked = true;
                        ShowThermalLinesCheckBox.IsChecked = true;
                        ShowThermalTotalsCheckBox.IsChecked = true;
                        CutAfterPrintCheckBox.IsChecked = true;
                    }
                    else
                    {
                        A4_Click(null, null);
                    }

                    DecimalPlacesComboBox.SelectedIndex = 2; // 2 decimal places
                    ShowLogoCheckBox.IsChecked = false;
                    ShowBarcodeCheckBox.IsChecked = false;
                    ColorPrintCheckBox.IsChecked = true;
                    ShowCashierCheckBox.IsChecked = true;

                    StatusText.Text = "✅ تم إعادة التعيين إلى الافتراضي";

                    MessageBox.Show(
                        "تم إعادة تعيين جميع الإعدادات إلى الافتراضي",
                        "تم بنجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ResetButton_Click Error: {ex.Message}");
                MessageBox.Show(
                    $"خطأ في إعادة التعيين:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// تطبيق Preset: متجر صغير (58mm)
        /// </summary>
        private void ApplyPreset_SmallShop(object sender, RoutedEventArgs e)
        {
            try
            {
                Simple_Click(null, null);
                Width58_Click(null, null);
                FontSizeComboBox.SelectedIndex = 0; // 10pt - صغير
                DensitySlider.Value = 3; // داكن للوضوح
                DecimalPlacesComboBox.SelectedIndex = 2; // 2
                ShowThermalHeaderCheckBox.IsChecked = true;
                ShowThermalLinesCheckBox.IsChecked = true;
                ShowThermalTotalsCheckBox.IsChecked = false; // مبسّط
                CutAfterPrintCheckBox.IsChecked = true;

                StatusText.Text = "✅ تم تطبيق إعدادات متجر صغير";

                MessageBox.Show(
                    "✅ تم تطبيق إعدادات متجر صغير:\n\n" +
                    "• قالب: Simple\n" +
                    "• عرض الورق: 58mm\n" +
                    "• حجم الخط: صغير (10pt)\n" +
                    "• كثافة: داكن (3)\n" +
                    "• مناسب للمتاجر الصغيرة والأكشاك",
                    "تم التطبيق",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ApplyPreset_SmallShop Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق Preset: محل عادي (80mm)
        /// </summary>
        private void ApplyPreset_MediumShop(object sender, RoutedEventArgs e)
        {
            try
            {
                Simple_Click(null, null);
                Width80_Click(null, null);
                FontSizeComboBox.SelectedIndex = 1; // 12pt - عادي
                DensitySlider.Value = 2; // متوسط
                DecimalPlacesComboBox.SelectedIndex = 2; // 2
                ShowThermalHeaderCheckBox.IsChecked = true;
                ShowThermalLinesCheckBox.IsChecked = true;
                ShowThermalTotalsCheckBox.IsChecked = true;
                CutAfterPrintCheckBox.IsChecked = true;

                StatusText.Text = "✅ تم تطبيق إعدادات محل عادي";

                MessageBox.Show(
                    "✅ تم تطبيق إعدادات محل عادي:\n\n" +
                    "• قالب: Simple\n" +
                    "• عرض الورق: 80mm\n" +
                    "• حجم الخط: عادي (12pt)\n" +
                    "• كثافة: متوسط (2)\n" +
                    "• مناسب للمحلات والسوبرماركت الصغيرة",
                    "تم التطبيق",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ApplyPreset_MediumShop Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تطبيق Preset: مؤسسة (A4)
        /// </summary>
        private void ApplyPreset_Company(object sender, RoutedEventArgs e)
        {
            try
            {
                Professional_Click(null, null);
                A4_Click(null, null);
                DecimalPlacesComboBox.SelectedIndex = 2; // 2
                ColorPrintCheckBox.IsChecked = true;
                ShowCashierCheckBox.IsChecked = true;
                ShowLogoCheckBox.IsChecked = false;
                ShowBarcodeCheckBox.IsChecked = false;

                StatusText.Text = "✅ تم تطبيق إعدادات مؤسسة";

                MessageBox.Show(
                    "✅ تم تطبيق إعدادات مؤسسة:\n\n" +
                    "• قالب: Professional\n" +
                    "• حجم الورقة: A4\n" +
                    "• طباعة ملونة: نعم\n" +
                    "• اسم الكاشير: نعم\n" +
                    "• مناسب للشركات والمؤسسات",
                    "تم التطبيق",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ApplyPreset_Company Error: {ex.Message}");
            }
        }

        /// <summary>
        /// إغلاق النافذة
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// إنشاء فاتورة تجريبية
        /// </summary>
        private Sale CreateTestSale()
        {
            var sale = new Sale
            {
                Id = 99999,
                SaleNumber = "TEST-2024-99999",
                SaleDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Cashier = "مستخدم تجريبي",
                RegisterId = "POS-DEMO",
                TaxRate = 0m,
                Notes = "فاتورة تجريبية لاختبار التخصيصات"
            };

            // منتجات متنوعة
            sale.Items.Add(new SaleItem
            {
                ProductId = 1,
                Name = "منتج قصير",
                Quantity = 1,
                UnitPrice = 25.00m,
                UnitName = "قطعة"
            });

            sale.Items.Add(new SaleItem
            {
                ProductId = 2,
                Name = "منتج بإسم طويل جداً لاختبار كيفية ظهوره في الفاتورة",
                Quantity = 2.5m,
                UnitPrice = 150.75m,
                UnitName = "كيلو"
            });

            sale.Items.Add(new SaleItem
            {
                ProductId = 3,
                Name = "منتج متوسط الطول",
                Quantity = 10,
                UnitPrice = 12.99m,
                UnitName = "علبة"
            });

            sale.CalculateTotals();
            sale.AmountPaid = sale.Total;

            return sale;
        }

        /// <summary>
        /// معالجة ضغط المفاتيح
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                CloseButton_Click(this, null);
            }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveButton_Click(this, null);
            }
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                PreviewButton_Click(this, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// تحديث حالة النافذة مع الأيقونة
        /// </summary>
        private void UpdateStatus(string message, string iconKind = "InformationOutline", string color = "#6366F1", bool showProgress = false)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = message;

                    if (ProgressIndicator != null && StatusIcon != null)
                    {
                        if (showProgress)
                        {
                            ProgressIndicator.Visibility = Visibility.Visible;
                            StatusIcon.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ProgressIndicator.Visibility = Visibility.Collapsed;
                            StatusIcon.Visibility = Visibility.Visible;

                            if (Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(iconKind, out var kind))
                            {
                                StatusIcon.Kind = kind;
                            }

                            StatusIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateStatus Error: {ex.Message}");
            }
        }
    }
}

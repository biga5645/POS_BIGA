using POS_BIGA.Infrastructure.Printing.Services;
using System.Windows;

namespace POS_BIGA.Infrastructure.Printing.Views
{
    public partial class SimpleTemplateWindow : Window
    {
        public SimpleTemplateWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = PrinterService.Instance.LoadSettings();
            WidthComboBox.SelectedIndex = settings.ReceiptWidthMm == 58 ? 0 : 1;
            FontSizeComboBox.SelectedIndex = settings.ThermalFontSize switch { 10 => 0, 14 => 2, _ => 1 };
            DensitySlider.Value = settings.ThermalDensity >= 1 && settings.ThermalDensity <= 3 ? settings.ThermalDensity : 2;
            DecimalPlacesComboBox.SelectedIndex = settings.DecimalPlaces >= 0 && settings.DecimalPlaces <= 4 ? settings.DecimalPlaces : 2;
            ShowHeaderCheckBox.IsChecked   = settings.ShowThermalHeader;
            ShowLinesCheckBox.IsChecked    = settings.ShowThermalLines;
            ShowTotalsCheckBox.IsChecked   = settings.ShowThermalTotals;
            CutAfterPrintCheckBox.IsChecked = settings.CutAfterPrint;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = PrinterService.Instance.LoadSettings();
            settings.TemplateName   = "Simple";
            settings.ReceiptWidthMm = WidthComboBox.SelectedIndex == 0 ? 58 : 80;
            settings.ThermalFontSize = FontSizeComboBox.SelectedIndex switch { 0 => 10, 2 => 14, _ => 12 };
            settings.ThermalDensity = (int)DensitySlider.Value;
            settings.DecimalPlaces  = DecimalPlacesComboBox.SelectedIndex >= 0 ? DecimalPlacesComboBox.SelectedIndex : 2;
            settings.ShowThermalHeader = ShowHeaderCheckBox.IsChecked == true;
            settings.ShowThermalLines  = ShowLinesCheckBox.IsChecked == true;
            settings.ShowThermalTotals = ShowTotalsCheckBox.IsChecked == true;
            settings.CutAfterPrint     = CutAfterPrintCheckBox.IsChecked == true;
            PrinterService.Instance.SaveSettings(settings);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

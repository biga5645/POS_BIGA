using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Infrastructure.Printing.Settings;
using System.Windows;

namespace POS_BIGA.Infrastructure.Printing.Views
{
    public partial class ProfessionalTemplateWindow : Window
    {
        public ProfessionalTemplateWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var settings = PrinterService.Instance.LoadSettings();
            PaperSizeComboBox.SelectedIndex    = settings.PaperSize == PaperSize.A5 ? 1 : 0;
            DecimalPlacesComboBox.SelectedIndex = settings.DecimalPlaces >= 0 && settings.DecimalPlaces <= 4 ? settings.DecimalPlaces : 2;
            ShowLogoCheckBox.IsChecked    = settings.ShowLogo;
            ShowCashierCheckBox.IsChecked = settings.ShowCashier;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = PrinterService.Instance.LoadSettings();
            settings.TemplateName  = "Professional A4/A5";
            settings.PaperSize     = PaperSizeComboBox.SelectedIndex == 1 ? PaperSize.A5 : PaperSize.A4;
            settings.DecimalPlaces = DecimalPlacesComboBox.SelectedIndex >= 0 ? DecimalPlacesComboBox.SelectedIndex : 2;
            settings.ShowLogo      = ShowLogoCheckBox.IsChecked == true;
            settings.ShowCashier   = ShowCashierCheckBox.IsChecked == true;
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

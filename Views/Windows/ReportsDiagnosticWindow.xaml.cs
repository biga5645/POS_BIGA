using System.Windows;
using POS_BIGA.Utilities;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// نافذة تشخيص بيانات التقارير - لحل مشكلة الأصفار
    /// </summary>
    public partial class ReportsDiagnosticWindow : Window
    {
        public ReportsDiagnosticWindow()
        {
            InitializeComponent();
        }

        private async void VerifyData_Click(object sender, RoutedEventArgs e)
        {
            VerifyButton.IsEnabled = false;
            ResultTextBlock.Text = "⏳ جاري التحقق من البيانات...";

            var result = await ReportsDataVerifier.VerifyDataAsync();
            ResultTextBlock.Text = result;

            VerifyButton.IsEnabled = true;
        }

        private async void CreateTestData_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = MessageBox.Show(
                "هل تريد إنشاء بيانات اختبار؟\nسيتم إضافة فاتورة اختبار جديدة.",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirmResult == MessageBoxResult.Yes)
            {
                CreateTestButton.IsEnabled = false;
                ResultTextBlock.Text = "⏳ جاري إنشاء بيانات الاختبار...";

                var result = await ReportsDataVerifier.CreateTestDataAsync();
                ResultTextBlock.Text += "\n\n" + result;

                CreateTestButton.IsEnabled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

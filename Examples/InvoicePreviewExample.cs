using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Models;
using System.Windows;

namespace POS_BIGA.Examples
{
    /// <summary>
    /// أمثلة على كيفية استخدام نافذة معاينة الفاتورة
    /// </summary>
    public class InvoicePreviewExample
    {
        /// <summary>
        /// مثال 1: فتح معاينة الفاتورة مع الإعدادات الافتراضية
        /// </summary>
        public static void ShowPreviewWithDefaultSettings(Sale sale)
        {
            try
            {
                var printerService = PrinterService.Instance;
                printerService.ShowPreviewWindow(sale);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"فشل فتح معاينة الفاتورة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// مثال 2: فتح معاينة الفاتورة مع إعدادات مخصصة
        /// </summary>
        public static void ShowPreviewWithCustomSettings(Sale sale)
        {
            try
            {
                var printerService = PrinterService.Instance;
                var settings = printerService.LoadSettings();
                
                // تخصيص الإعدادات
                settings.PaperSize = POS_BIGA.Infrastructure.Printing.Settings.PaperSize.A5;
                settings.TemplateName = "Professional A4/A5";
                
                printerService.ShowPreviewWindow(sale, settings);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"فشل فتح معاينة الفاتورة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// مثال 3: فتح معاينة مباشرة من زر في الواجهة
        /// </summary>
        /// <example>
        /// في XAML:
        /// <Button Content="معاينة الفاتورة" Click="PreviewButton_Click"/>
        /// 
        /// في Code-Behind:
        /// private void PreviewButton_Click(object sender, RoutedEventArgs e)
        /// {
        ///     if (_currentSale != null)
        ///     {
        ///         InvoicePreviewExample.ShowPreviewWithDefaultSettings(_currentSale);
        ///     }
        /// }
        /// </example>

        /// <summary>
        /// مثال 4: معاينة قبل الطباعة
        /// </summary>
        public static async System.Threading.Tasks.Task PreviewBeforePrint(Sale sale)
        {
            var printerService = PrinterService.Instance;
            
            // عرض المعاينة أولاً
            printerService.ShowPreviewWindow(sale);
            
            // يمكن للمستخدم الطباعة من نافذة المعاينة
            // أو يمكنك الطباعة مباشرة بعد إغلاق النافذة:
            
            // var result = MessageBox.Show(
            //     "هل تريد طباعة الفاتورة الآن؟",
            //     "طباعة",
            //     MessageBoxButton.YesNo,
            //     MessageBoxImage.Question
            // );
            
            // if (result == MessageBoxResult.Yes)
            // {
            //     await printerService.PrintAsync(sale);
            // }
        }

        /// <summary>
        /// مثال 5: معاينة فاتورة معينة من قاعدة البيانات
        /// </summary>
        public static async System.Threading.Tasks.Task ShowPreviewForSaleId(int saleId)
        {
            try
            {
                using var ctx = new POS_BIGA.Data.AppDbContext();
                var sale = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .FirstOrDefaultAsync(
                        Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                            .Include(ctx.Sales, s => s.Items),
                        s => s.Id == saleId);

                if (sale != null)
                {
                    ShowPreviewWithDefaultSettings(sale);
                }
                else
                {
                    MessageBox.Show(
                        $"لم يتم العثور على فاتورة برقم {saleId}",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"خطأ في تحميل الفاتورة:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}

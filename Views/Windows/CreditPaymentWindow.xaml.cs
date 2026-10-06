using System.Windows;
using System.Windows.Documents;
using POS_BIGA.ViewModels;
using POS_BIGA.Infrastructure.Printing.Services;
using POS_BIGA.Infrastructure.Printing.Settings;
using System;
using System.Windows.Media;

namespace POS_BIGA.Views.Windows
{
    /// <summary>
    /// MVVM Strict - Minimal Code-Behind for Printing
    /// </summary>
    public partial class CreditPaymentWindow : Window
    {
        public CreditPaymentWindow(int customerId, string customerName)
        {
            InitializeComponent();

            var vm = new CreditPaymentViewModel(customerId, customerName);
            vm.RequestClose += (success) =>
            {
                DialogResult = success;
                Close();
            };
            vm.RequestPrint += () => PrintReceipt(vm);
            DataContext = vm;
        }

        private void PrintReceipt(CreditPaymentViewModel vm)
        {
            try
            {
                // ✅ Use PrinterService.Instance Singleton
                var printerService = PrinterService.Instance;
                var settings = printerService.LoadSettings();

                System.Diagnostics.Debug.WriteLine("═══════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🖨️ Credit Payment Receipt");
                System.Diagnostics.Debug.WriteLine($"ShopName: '{settings.ShopName}'");
                System.Diagnostics.Debug.WriteLine($"ShopAddress: '{settings.ShopAddress}'");
                System.Diagnostics.Debug.WriteLine($"ReceiptPrinter: '{settings.ReceiptPrinter}'");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════");

                // ✅ Build document with shop info from settings
                var doc = BuildPaymentReceiptDocument(vm, settings);

                // ✅ Print to ReceiptPrinter (NO default fallback)
                if (!string.IsNullOrEmpty(settings.ReceiptPrinter))
                {
                    try
                    {
                        printerService.PrintDocument(doc, settings.ReceiptPrinter);
                        MessageBox.Show("✅ تم إرسال الإيصال إلى الطابعة بنجاح!", 
                            "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Direct print failed: {ex.Message}");
                        ShowPrintDialog(doc);
                    }
                }
                else
                {
                    ShowPrintDialog(doc);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Print error: {ex.Message}");
                MessageBox.Show($"❌ خطأ في الطباعة:\n{ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument BuildPaymentReceiptDocument(CreditPaymentViewModel vm, PrintSettings settings)
        {
            var doc = new FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Arial"),
                FontSize = 11,
                PagePadding = new Thickness(20),
                FlowDirection = FlowDirection.RightToLeft,
                PageWidth = (settings?.ReceiptWidthMm ?? 80) * (96.0 / 25.4)
            };

            // ═══════════════════════════════════════
            // ✅ SHOP HEADER FROM SETTINGS
            // ═══════════════════════════════════════

            if (!string.IsNullOrEmpty(settings?.ShopName))
            {
                doc.Blocks.Add(new Paragraph(new Run(settings.ShopName))
                {
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            if (!string.IsNullOrEmpty(settings?.ShopAddress))
            {
                doc.Blocks.Add(new Paragraph(new Run(settings.ShopAddress))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            if (!string.IsNullOrEmpty(settings?.ShopPhone))
            {
                doc.Blocks.Add(new Paragraph(new Run($"📞 {settings.ShopPhone}"))
                {
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // Title
            doc.Blocks.Add(new Paragraph(new Run("إيصال تسديد ديون"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                Margin = new Thickness(0, 4, 0, 2)
            });

            doc.Blocks.Add(new Paragraph(new Run("PAYMENT RECEIPT"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            doc.Blocks.Add(Separator());

            // Receipt Info
            string receiptId = $"RCP-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
            doc.Blocks.Add(new Paragraph(new Run($"رقم الإيصال: {receiptId}"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 2)
            });

            doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 8)
            });

            doc.Blocks.Add(Separator());

            // Customer
            doc.Blocks.Add(new Paragraph(new Run($"العميل: {vm.CustomerName}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 2)
            });

            if (!string.IsNullOrWhiteSpace(vm.Notes))
            {
                doc.Blocks.Add(new Paragraph(new Run($"ملاحظات: {vm.Notes}"))
                {
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 2, 0, 8)
                });
            }

            doc.Blocks.Add(Separator());

            // Debt Summary
            doc.Blocks.Add(new Paragraph(new Run("📊 ملخص الحساب"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 6)
            });

            string currency = settings?.Currency ?? "د.م";
            doc.Blocks.Add(new Paragraph(new Run($"إجمالي الديون السابقة: {vm.TotalDebt:N2} {currency}"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 2)
            });

            doc.Blocks.Add(new Paragraph(new Run($"عدد الفواتير المتبقية: {vm.UnpaidInvoicesCount}"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 8)
            });

            doc.Blocks.Add(Separator());

            // Payment Details
            doc.Blocks.Add(new Paragraph(new Run("💰 تفاصيل الدفعة الحالية"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                Margin = new Thickness(0, 4, 0, 6)
            });

            doc.Blocks.Add(new Paragraph(new Run($"المبلغ المدفوع الآن: {vm.AmountToPay:N2} {currency}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                Margin = new Thickness(0, 2, 0, 2)
            });

            doc.Blocks.Add(new Paragraph(new Run($"طريقة الدفع: {vm.SelectedPaymentMethod}"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 8)
            });

            doc.Blocks.Add(Separator());

            // Status After Payment
            doc.Blocks.Add(new Paragraph(new Run("✅ الحالة بعد الدفع"))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 6)
            });

            doc.Blocks.Add(new Paragraph(new Run($"المتبقي بذمة العميل: {vm.RemainingAfter:N2} {currency}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = vm.RemainingAfter > 0 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)),
                Margin = new Thickness(0, 2, 0, 2)
            });

            decimal percentage = vm.TotalDebt > 0 ? (vm.AmountToPay / vm.TotalDebt * 100) : 0;
            doc.Blocks.Add(new Paragraph(new Run($"نسبة سداد هذه الدفعة: {percentage:N1}%"))
            {
                FontSize = 10,
                Margin = new Thickness(0, 2, 0, 8)
            });

            doc.Blocks.Add(DoubleSeparator());

            // Footer
            doc.Blocks.Add(new Paragraph(new Run($"المستلم: {POS_BIGA.Services.SessionService.CurrentUserDisplayName}"))
            {
                FontSize = 9,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            });

            doc.Blocks.Add(new Paragraph(new Run("طريقة التوزيع: الأقدم أولاً (FIFO)"))
            {
                FontSize = 9,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 2, 0, 8)
            });

            doc.Blocks.Add(Separator());

            // Thank you message
            string thanks = !string.IsNullOrEmpty(settings?.InvoiceMessage) 
                ? settings.InvoiceMessage 
                : "شكراً لثقتكم بنا\nنقدر تعاملكم الدائم";

            doc.Blocks.Add(new Paragraph(new Run(thanks))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 6)
            });

            doc.Blocks.Add(Separator());

            return doc;
        }

        private Paragraph Separator()
        {
            return new Paragraph(new Run(new string('-', 40)))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        private Paragraph DoubleSeparator()
        {
            return new Paragraph(new Run(new string('═', 40)))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 6)
            };
        }

        private void ShowPrintDialog(FlowDocument doc)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    printDialog.PrintDocument(paginator, "إيصال تسديد دين");
                    MessageBox.Show("✅ تم الطباعة بنجاح!", 
                        "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ PrintDialog error: {ex.Message}");
                MessageBox.Show($"❌ خطأ في الطباعة:\n{ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

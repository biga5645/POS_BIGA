namespace POS_BIGA.Infrastructure.Printing.Settings
{
    /// <summary>
    /// 📄 أحجام الورق المدعومة
    /// </summary>
    public enum PaperSize
    {
        A4,    // 210mm × 297mm
        A5     // 148mm × 210mm
    }

    public class PrintSettings
    {
        public string ShopName { get; set; } = string.Empty;
        public string ShopAddress { get; set; } = string.Empty;
        public string ShopPhone { get; set; } = string.Empty;
        public string Currency { get; set; } = "MAD";
        public string TemplateName { get; set; } = "Simple";
        public int ReceiptWidthMm { get; set; } = 80;
        public decimal TaxRate { get; set; } = 0m;
        public string ReceiptPrinter { get; set; } = string.Empty;
        public string InvoiceMessage { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = "?.?";
        public bool AutoPrint { get; set; } = false;
        public bool ForceEscPos { get; set; } = false;

        /// <summary>
        /// ✅ حجم الورقة للطباعة (A4 أو A5)
        /// </summary>
        public PaperSize PaperSize { get; set; } = PaperSize.A4;

        /// <summary>
        /// 🖨️ إعدادات الطابعة الحرارية (Simple Template)
        /// </summary>
        public int ThermalFontSize { get; set; } = 12; // 10, 12, 14
        public int ThermalDensity { get; set; } = 2; // 1-3
        public bool ShowThermalHeader { get; set; } = true;
        public bool ShowThermalLines { get; set; } = true;
        public bool ShowThermalTotals { get; set; } = true;
        public bool CutAfterPrint { get; set; } = true;

        // copy/paste of earlier fields to keep compatibility for consumers
        public int Copies { get; set; } = 1;
        public bool CutPaper { get; set; } = false;
        public bool ShowLogo { get; set; } = true;
        public string LogoPath { get; set; } = string.Empty;
        public bool ShowItemCode { get; set; } = false;
        public int DecimalPlaces { get; set; } = 2;
        public bool ShowCashier { get; set; } = false;
        public bool ShowDateTime { get; set; } = true;

        // ─── أقسام تقرير الطباعة الحرارية ───────────────────────────
        public bool ReportShowHeader       { get; set; } = true;
        public bool ReportShowSummary      { get; set; } = true;
        public bool ReportShowTopProducts  { get; set; } = true;
        public bool ReportShowCategories   { get; set; } = true;
        public bool ReportShowDailySales   { get; set; } = true;
        public bool ReportShowCredits      { get; set; } = true;
        public bool ReportShowCreditsList  { get; set; } = false;

        // ─── أقسام تقرير A4/A5 ──────────────────────────────────────
        public bool A4ReportShowHeader      { get; set; } = true;
        public bool A4ReportShowSummary     { get; set; } = true;
        public bool A4ReportShowTopProducts { get; set; } = true;
        public bool A4ReportShowCategories  { get; set; } = true;
        public bool A4ReportShowDailySales  { get; set; } = true;
        public bool A4ReportShowCredits     { get; set; } = true;
        public bool A4ReportShowCreditsList { get; set; } = false;

        // ─── خصائص مشتركة جديدة ─────────────────────────────────────
        // حراري
        public bool ReportShowSoldProducts   { get; set; } = false;
        public bool ReportShowCreditDetails  { get; set; } = false;
        // A4
        public bool A4ReportShowSoldProducts  { get; set; } = false;
        public bool A4ReportShowCreditDetails { get; set; } = false;
    }
}

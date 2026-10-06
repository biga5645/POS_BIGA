namespace POS_BIGA.Infrastructure.Printing.Engines
{
    public interface IPrinterEngine
    {
        string Name { get; }
        bool CanPrintTo(string printerName);
        void PrintRaw(byte[] data, string printerName);
    }
}

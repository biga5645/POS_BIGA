using System.Windows.Documents;
using POS_BIGA.Infrastructure.Printing.Settings;
using POS_BIGA.Models;

namespace POS_BIGA.Infrastructure.Printing.Abstractions
{
    public interface IPrintTemplate
    {
        string Name { get; }

        /// <summary>WPF FlowDocument — للمعاينة داخل WPF</summary>
        FlowDocument BuildPreview(Sale sale, PrintSettings settings);

        /// <summary>PDF bytes — للطباعة والتصدير والحفظ (نفس القالب دائماً)</summary>
        byte[] BuildPdf(Sale sale, PrintSettings settings);

        byte[] BuildRaw(Sale sale, PrintSettings settings);
    }
}

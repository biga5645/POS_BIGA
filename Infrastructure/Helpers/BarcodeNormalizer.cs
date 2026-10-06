using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace POS_BIGA.Infrastructure.Helpers
{
    /// <summary>
    /// Helper to normalize user input (Arabic / French / accented) into ASCII suitable for barcodes.
    /// </summary>
    public static class BarcodeHelper
    {
        // minimum length to treat input as a complete barcode scan
        public const int MinBarcodeScanLength = 6;

        /// <summary>
        /// Fix common symbols produced by non-English keyboard layouts to intended digits/letters.
        /// Converts Arabic/French keyboard symbols and Arabic-Indic digits to ASCII.
        /// </summary>
        public static string FixBarcodeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                sb.Append(c switch
                {
                    // French AZERTY digit-row symbols → digits
                    '&'  => '1',
                    'é'  => '2',
                    '"'  => '3',
                    '\'' => '4',
                    '('  => '5',
                    '-'  => '6',
                    'è'  => '7',
                    '_'  => '8',
                    'ç'  => '9',
                    'à'  => '0',
                    '!'  => '1',
                    // Arabic-Indic digits → Western
                    '٠'  => '0',
                    '١'  => '1',
                    '٢'  => '2',
                    '٣'  => '3',
                    '٤'  => '4',
                    '٥'  => '5',
                    '٦'  => '6',
                    '٧'  => '7',
                    '٨'  => '8',
                    '٩'  => '9',
                    _    => c
                });
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns true if the string looks like a complete barcode scan
        /// (only digits/dashes, minimum length met).
        /// </summary>
        public static bool IsCompleteScan(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var normalized = FixBarcodeText(text.Trim());
            return normalized.Length >= MinBarcodeScanLength
                && normalized.All(c => char.IsDigit(c) || c == '-');
        }

        /// <summary>
        /// Normalize and trim the text, then return it ready for DB lookup.
        /// Returns empty string if input is null/whitespace.
        /// </summary>
        public static string Normalize(string text)
            => FixBarcodeText((text ?? string.Empty).Trim());
    }
}

using System;

namespace BS.CAD.Tools.Models
{
    public class InputLanguageItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string CultureName { get; set; } = string.Empty;
        public string HklHex { get; set; } = string.Empty;
        public IntPtr Handle { get; set; } = IntPtr.Zero;

        public override string ToString() => DisplayName;

        public static string FormatHkl(IntPtr handle)
        {
            try { return $"0x{handle.ToInt64():X8}"; }
            catch { return ""; }
        }

        public static IntPtr ParseHkl(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return IntPtr.Zero;
            try
            {
                if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return new IntPtr(long.Parse(hex[2..], System.Globalization.NumberStyles.HexNumber));
                return new IntPtr(long.Parse(hex, System.Globalization.NumberStyles.HexNumber));
            }
            catch { return IntPtr.Zero; }
        }
    }
}

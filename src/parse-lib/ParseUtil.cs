using System;
using System.Globalization;

namespace parse_lib
{
    public static class ParseUtil
    {
        public static int? IntTryParse(string text)
        {
            if (text == null) { return null; }
            if (string.IsNullOrWhiteSpace(text)) { return null; }

            int value;
            return int.TryParse(text, out value)
                ? value
                : (int?)null;
        }

        public static decimal? DecimalTryParse(string text)
        {
            if (text == null) { return null; }
            var effectiveText = text.Trim().Replace(",", string.Empty);

            return !string.IsNullOrWhiteSpace(effectiveText) && decimal.TryParse(effectiveText, NumberStyles.Float, CultureInfo.CurrentCulture, out decimal value)
                ? value
                : (decimal?)null;
        }

        public static double? DoubleTryParse(string text)
        {
            if (text == null) { return null; }
            var effectiveText = text.Trim().Replace(",", string.Empty);

            return !string.IsNullOrWhiteSpace(effectiveText) && double.TryParse(effectiveText, NumberStyles.Float, CultureInfo.CurrentCulture, out double value)
                ? value
                : (double?)null;
        }

        public static DateTime? DateTimeTryParse(string text, CultureInfo culture = null)
        {
            var effectiveCulutre = culture ?? CultureInfo.CurrentCulture;
            return !string.IsNullOrWhiteSpace(text) && DateTime.TryParse(text, effectiveCulutre, DateTimeStyles.None, out DateTime value)
                ? value
                : (DateTime?)null;
        }

        public static Guid? GuidTryParse(string text)
        {
            return Guid.TryParse(text, out Guid id) ? id : (Guid?)null;
        }        
    }
}

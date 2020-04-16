using System.Globalization;

namespace EnhancedSearchAndFilters.Utilities
{
    public static class StringUtilities
    {
        public static string InvariantToString(this object obj)
        {
            if (obj is float floatValue)
                return floatValue.ToString(CultureInfo.InvariantCulture);
            else if (obj is double doubleValue)
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            else
                return obj.ToString();
        }

        public static bool TryParseInvariantFloat(string s, out float floatValue)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue);
        }

        public static bool TryParseInvariantDouble(string s, out double doubleValue)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue);
        }
    }
}

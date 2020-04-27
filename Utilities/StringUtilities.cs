using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// Escape characters in a <see cref="StringBuilder"/> according to a provided mapping.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> that holds a string with characters to escape.</param>
        /// <param name="escapeChar">The character that is used as a prefix to signify an escape sequence.</param>
        /// <param name="mapping">A mapping from the <see cref="char"/> that needs to be escaped to a <see cref="char"/> that will be used to signify the original character.</param>
        /// <returns>The <see cref="StringBuilder"/> itself.</returns>
        public static StringBuilder EscapeString(this StringBuilder sb, char escapeChar, Dictionary<char, char> mapping)
        {
            // escape the escape char
            // has to be done first
            sb.Replace(escapeChar.ToString(), $"{escapeChar}{escapeChar}");

            foreach (char toEscape in mapping.Keys)
                sb.Replace(toEscape.ToString(), $"{escapeChar}{mapping[toEscape]}");

            return sb;
        }

        /// <summary>
        /// Unescape characters in a <see cref="StringBuilder"/> according to a provided mapping.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> that holds an escaped string that should be decoded to get the original string.</param>
        /// <param name="escapeChar">The character that is used as a prefix to signify an escape sequence.</param>
        /// <param name="mapping">A mapping from the <see cref="char"/> that needs to be escaped to a <see cref="char"/> that will be used to signify the original character.</param>
        /// <returns>The <see cref="StringBuilder"/> itself.</returns>
        public static StringBuilder UnescapeString(this StringBuilder sb, char escapeChar, Dictionary<char, char> mapping)
        {
            for (int i = 0; i < sb.Length; ++i)
            {
                if (sb[i] == escapeChar && (i + 1) < sb.Length)
                {
                    char escapeCharCode = sb[i + 1];

                    if (mapping.ContainsValue(escapeCharCode))
                    {
                        sb.Replace($"{escapeChar}{escapeCharCode}", mapping.First(kv => kv.Value == escapeCharCode).Key.ToString(), i, 1);
                    }
                    else
                    {
                        sb.Remove(i, 1);
                        --i;
                    }
                }
                else if (sb[i] == escapeChar)
                {
                    sb.Remove(i, 1);
                }
            }

            return sb;
        }
    }
}

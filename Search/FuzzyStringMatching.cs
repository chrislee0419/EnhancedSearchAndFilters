using System;

namespace EnhancedSearchAndFilters.Search
{
    public static class FuzzyStringMatching
    {
        /// <summary>
        /// Get the Levenshtein edit distance between two strings.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>The edit distance between the two provided strings.</returns>
        public static int LevenshteinDistance(string s1, string s2)
        {
            if (s1.Length == 0)
                return s2.Length;
            else if (s2.Length == 0)
                return s1.Length;

            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int i = 0; i <= s2.Length; i++)
                d[0, i] = i;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int match = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + match);
                }
            }

            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// Get the similarity value of two strings using the Jaro-Winkler similarity.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="scalingFactor">A number signifying how much additional value is placed on a longer common prefix.</param>
        /// <param name="maxCommonPrefix">The longest length common prefix allowed to be used during scaling.</param>
        /// <returns>A number between 0 and 1 which indicates how similar the two provided strings are, where 1 represets the two strings being the same.</returns>
        public static float JaroWinklerSimilarity(string s1, string s2, float scalingFactor = 0.1f, int maxCommonPrefix = 4)
        {
            // adapted from: https://stackoverflow.com/a/19165108
            if (string.IsNullOrEmpty(s1))
                return string.IsNullOrEmpty(s2) ? 1f : 0f;

            // get number of matched characters (m)
            float m = 0;
            int matchDistance = Convert.ToInt32(Math.Floor(Math.Max(s1.Length, s2.Length) / 2f)) - 1;
            bool[] matchedCharacters1 = new bool[s1.Length];
            bool[] matchedCharacters2 = new bool[s2.Length];
            for (int i = 0; i < s1.Length; ++i)
            {
                int left = Math.Max(0, i - matchDistance);
                int right = Math.Min(s2.Length - 1, i + matchDistance);

                for (int j = left; j <= right; ++j)
                {
                    if (s1[i] != s2[j] || matchedCharacters2[j])
                        continue;

                    matchedCharacters1[i] = true;
                    matchedCharacters2[j] = true;
                    ++m;

                    break;
                }
            }

            if (m == 0)
                return 0;

            // get number of transpositions (t)
            float t = 0;
            for (int i = 0, j = 0; i < s1.Length; ++i)
            {
                if (!matchedCharacters1[i])
                    continue;

                while (!matchedCharacters2[j])
                    ++j;

                if (s1[i] != s2[j])
                    ++t;

                ++j;
            }
            t /= 2;

            float jaro = ((m / s1.Length) + (m / s2.Length) + ((m - t) / m)) / 3;

            // get length of common prefix (l)
            int l = 0;
            for (int i = 0; i < s1.Length && i < s2.Length && i < maxCommonPrefix; ++i)
            {
                if (s1[i] == s2[i])
                    ++l;
                else
                    break;
            }

            return jaro + l * scalingFactor * (1 - jaro);
        }
    }
}

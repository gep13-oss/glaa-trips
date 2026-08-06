using System.Globalization;
using System.Text;

namespace GlaaTrips.Models
{
    /// <summary>
    /// Produces URL- and filesystem-safe album slugs from human-entered titles,
    /// replacing the former <c>Slugify.Core</c> dependency at its single use site.
    /// A title is lower-cased, stripped of diacritics (so "Zürich" becomes
    /// "zurich"), and reduced to ASCII letters and digits with any run of other
    /// characters collapsed to a single separating hyphen. Leading and trailing
    /// hyphens are trimmed. A title with no slug-able characters yields an empty
    /// string, which the caller rejects via
    /// <see cref="SafePathHelper.IsValidSegment(string)"/> before it is used as a
    /// directory name.
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Generates a slug of lower-case ASCII letters, digits and single hyphens
        /// from <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The human-entered title to slugify.</param>
        /// <returns>
        /// The slug, or an empty string when <paramref name="value"/> is null,
        /// blank, or contains no letters or digits.
        /// </returns>
        public static string GenerateSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // FormD splits accented letters into a base letter plus a combining
            // mark, so the marks can be dropped to fold the accent onto ASCII.
            var normalised = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalised.Length);
            var lastWasHyphen = false;

            foreach (var ch in normalised)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                {
                    builder.Append(ch);
                    lastWasHyphen = false;
                }
                else if (ch is >= 'A' and <= 'Z')
                {
                    builder.Append(char.ToLowerInvariant(ch));
                    lastWasHyphen = false;
                }
                else if (!lastWasHyphen && builder.Length > 0)
                {
                    // Any other character (space, punctuation, an unmapped symbol)
                    // becomes a single separating hyphen; consecutive runs collapse.
                    builder.Append('-');
                    lastWasHyphen = true;
                }
            }

            // A title ending in punctuation leaves a trailing hyphen to trim.
            if (lastWasHyphen && builder.Length > 0)
            {
                builder.Length--;
            }

            return builder.ToString();
        }
    }
}
using GlaaTrips.Models;

namespace GlaaTrips.UnitTests
{
    /// <summary>
    /// Coverage for <see cref="SlugHelper"/>, the inline album-slug generator that
    /// replaced the Slugify.Core dependency. The slug feeds
    /// <see cref="SafePathHelper.IsValidSegment(string)"/> and then becomes an
    /// album directory name, so these cases pin the contract the caller relies on:
    /// lower-case ASCII with single hyphens, folded diacritics, and an empty string
    /// for titles that slug to nothing.
    /// </summary>
    [TestFixture]
    public class SlugHelperTests
    {
        [TestCase("Sunset", "sunset")]
        [TestCase("Sunset 2", "sunset-2")]
        [TestCase("Edinburgh 2026", "edinburgh-2026")]
        [TestCase("already-a-slug", "already-a-slug")]
        [TestCase("UPPER CASE", "upper-case")]
        [TestCase("2026", "2026")]
        public void GenerateSlug_lower_cases_and_hyphenates_words(string input, string expected)
        {
            Assert.That(SlugHelper.GenerateSlug(input), Is.EqualTo(expected));
        }

        [TestCase("Café", "cafe")]
        [TestCase("Zürich", "zurich")]
        [TestCase("Åre", "are")]
        [TestCase("Reykjavík", "reykjavik")]
        [TestCase("Málaga trip", "malaga-trip")]
        public void GenerateSlug_folds_diacritics_to_ascii(string input, string expected)
        {
            Assert.That(SlugHelper.GenerateSlug(input), Is.EqualTo(expected));
        }

        [TestCase("Hello, World!", "hello-world")]
        [TestCase("a - b", "a-b")]
        [TestCase("one   two", "one-two")]
        [TestCase("dots.and_underscores", "dots-and-underscores")]
        [TestCase("emoji 😀 party", "emoji-party")]
        public void GenerateSlug_collapses_separators_and_punctuation_to_single_hyphens(string input, string expected)
        {
            Assert.That(SlugHelper.GenerateSlug(input), Is.EqualTo(expected));
        }

        [TestCase("  spaced  ", "spaced")]
        [TestCase("--dashed--", "dashed")]
        [TestCase("...trip...", "trip")]
        public void GenerateSlug_trims_leading_and_trailing_separators(string input, string expected)
        {
            Assert.That(SlugHelper.GenerateSlug(input), Is.EqualTo(expected));
        }

        // An all-punctuation or blank title slugs to nothing; the caller turns an
        // empty slug into a BadRequest rather than creating a bad album folder.
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("!!!")]
        [TestCase("...")]
        [TestCase("---")]
        public void GenerateSlug_returns_empty_when_nothing_is_slugable(string? input)
        {
            Assert.That(SlugHelper.GenerateSlug(input!), Is.Empty);
        }
    }
}
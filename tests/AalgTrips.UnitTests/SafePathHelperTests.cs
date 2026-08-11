using System;
using System.IO;
using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Direct coverage for <see cref="SafePathHelper"/>, the guard that stops
    /// user-supplied album names and photo names from escaping the albums web
    /// root. These run without a server so the boundary matrix can be exercised
    /// exhaustively and cheaply; the UITests suite proves the guard is wired into
    /// the handlers end-to-end.
    /// </summary>
    [TestFixture]
    public class SafePathHelperTests
    {
        [TestCase("sunset")]
        [TestCase("Sunset 2")]
        [TestCase("my-album")]
        [TestCase("photo_01")]
        [TestCase("edinburgh-2026")]
        [TestCase("holiday.snap")]
        [TestCase("café")]
        public void IsValidSegment_accepts_ordinary_names(string segment)
        {
            Assert.That(SafePathHelper.IsValidSegment(segment), Is.True);
        }

        // Separators, relative-navigation tokens, rooted paths and empty values
        // are unsafe on every platform.
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("../evil")]
        [TestCase("..\\evil")]
        [TestCase("foo/bar")]
        [TestCase("foo\\bar")]
        [TestCase("/etc/passwd")]
        [TestCase("\\\\server\\share")]
        [TestCase("a\0b")]
        public void IsValidSegment_rejects_traversal_and_separators(string? segment)
        {
            Assert.That(SafePathHelper.IsValidSegment(segment!), Is.False);
        }

        // Characters that are only invalid in a file name on Windows. Guarded so
        // the suite stays green if it ever runs on Linux, where these are legal.
        [TestCase("a:b")]
        [TestCase("a*b")]
        [TestCase("a?b")]
        [TestCase("a|b")]
        [TestCase("a<b")]
        [TestCase("a>b")]
        public void IsValidSegment_rejects_windows_invalid_filename_chars(string segment)
        {
            if (!OperatingSystem.IsWindows())
            {
                Assert.Ignore("These characters are only invalid in file names on Windows.");
            }

            Assert.That(SafePathHelper.IsValidSegment(segment), Is.False);
        }

        [Test]
        public void TryCombineWithin_returns_the_resolved_path_for_a_safe_segment()
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "albums");

            var ok = SafePathHelper.TryCombineWithin(baseDir, "sample-trip", out var full);

            Assert.That(ok, Is.True);
            Assert.That(full, Is.EqualTo(Path.GetFullPath(Path.Combine(baseDir, "sample-trip"))));
        }

        [TestCase("../evil")]
        [TestCase("..")]
        [TestCase("foo/bar")]
        [TestCase("/etc/passwd")]
        public void TryCombineWithin_rejects_an_escaping_segment(string segment)
        {
            var baseDir = Path.Combine(Path.GetTempPath(), "albums");

            var ok = SafePathHelper.TryCombineWithin(baseDir, segment, out var full);

            Assert.That(ok, Is.False);
            Assert.That(full, Is.Null);
        }
    }
}
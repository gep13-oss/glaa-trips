using System;
using System.IO;

namespace AalgTrips.Models
{
    /// <summary>
    /// Guards filesystem operations that are driven by user input against path
    /// traversal. Album names/slugs (route values) and photo names (form fields)
    /// flow into <see cref="Path.Combine(string, string)"/> in the admin handlers;
    /// without validation a value such as <c>../../secret</c> would let a request
    /// move, overwrite, or recursively delete files outside the albums web root.
    /// </summary>
    public static class SafePathHelper
    {
        /// <summary>
        /// Determines whether <paramref name="segment"/> is safe to use as a single
        /// path segment: non-empty, not a relative-navigation token, free of
        /// directory separators, not rooted, and containing no characters that are
        /// invalid in a file name.
        /// </summary>
        /// <param name="segment">The user-supplied value to validate.</param>
        /// <returns><c>true</c> when the value is a safe segment; otherwise <c>false</c>.</returns>
        public static bool IsValidSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            if (segment == "." || segment == "..")
            {
                return false;
            }

            // Reject separators explicitly on every platform, not just the ones the
            // current OS happens to honour (Linux treats '\' as an ordinary file-name
            // character, so relying on the framework alone would let it through).
            if (segment.IndexOf('/') >= 0 || segment.IndexOf('\\') >= 0)
            {
                return false;
            }

            if (Path.IsPathRooted(segment))
            {
                return false;
            }

            if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Combines <paramref name="baseDirectory"/> with a single user-supplied
        /// <paramref name="segment"/> and confirms the fully-resolved result stays
        /// inside the base directory. This is defence in depth on top of
        /// <see cref="IsValidSegment"/>: even if a separator slipped through, the
        /// resolved path is compared against the base before it is used.
        /// </summary>
        /// <param name="baseDirectory">The directory the result must stay within.</param>
        /// <param name="segment">The user-supplied path segment.</param>
        /// <param name="fullPath">The resolved absolute path when the method returns <c>true</c>.</param>
        /// <returns><c>true</c> when the segment is safe and resolves within the base directory.</returns>
        public static bool TryCombineWithin(string baseDirectory, string segment, out string fullPath)
        {
            fullPath = null;

            if (!IsValidSegment(segment))
            {
                return false;
            }

            var basePath = Path.GetFullPath(baseDirectory);
            var combined = Path.GetFullPath(Path.Combine(basePath, segment));

            var baseWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar)
                ? basePath
                : basePath + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            fullPath = combined;
            return true;
        }
    }
}
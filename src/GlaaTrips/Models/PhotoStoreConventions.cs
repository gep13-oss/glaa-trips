using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GlaaTrips.Models
{
    /// <summary>
    /// The naming and layout conventions shared by every <see cref="IPhotoStore"/>
    /// implementation, so a local-disk store and an Azure Blob store agree on the
    /// same key structure, thumbnail naming, and public URL encoding. Keys are
    /// always <c>{albumId}/…</c>: <c>{albumId}/data.json</c> for metadata,
    /// <c>{albumId}/{photo}</c> for an original, and
    /// <c>{albumId}/thumbnail/{thumb}</c> for a generated thumbnail; the marker
    /// file sits at the top level as <c>markers.json</c>.
    /// </summary>
    public static class PhotoStoreConventions
    {
        /// <summary>The sub-folder (key segment) generated thumbnails live under.</summary>
        public const string ThumbnailFolder = "thumbnail";

        /// <summary>The file name an album's metadata is stored as.</summary>
        public const string MetadataFileName = "data.json";

        /// <summary>The file name the map's marker list is stored as.</summary>
        public const string MarkersFileName = "markers.json";

        private static readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".gif", ".png" };
        private static readonly Regex _thumbnailSuffix = new Regex(@"-[0-9]+x[0-9]+$", RegexOptions.Compiled);

        /// <summary>
        /// Determines whether a file name is one of the image types the site
        /// accepts (by extension).
        /// </summary>
        /// <param name="fileName">The file name to test.</param>
        /// <returns><c>true</c> when the extension is a recognised image type.</returns>
        public static bool IsImageFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return _imageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Encodes a single album id or file name for use in a served URL,
        /// matching the site's long-standing convention of lower-casing and
        /// percent-encoding spaces so links are stable and case-insensitive.
        /// </summary>
        /// <param name="name">The raw album id or file name.</param>
        /// <returns>The URL-safe segment.</returns>
        public static string UrlSegment(string name)
        {
            return name.Replace(" ", "%20").ToLowerInvariant();
        }

        /// <summary>
        /// Determines whether a thumbnail file name was generated from a given
        /// original photo — i.e. it is <c>{name}-{width}x{height}{ext}</c>.
        /// </summary>
        /// <param name="thumbnailFileName">The thumbnail file name.</param>
        /// <param name="photoFileName">The original photo file name.</param>
        /// <returns><c>true</c> when the thumbnail belongs to the photo.</returns>
        public static bool ThumbnailBelongsTo(string thumbnailFileName, string photoFileName)
        {
            string photoName = Path.GetFileNameWithoutExtension(photoFileName);
            string photoExt = Path.GetExtension(photoFileName);

            if (!thumbnailFileName.EndsWith(photoExt, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string thumbName = Path.GetFileNameWithoutExtension(thumbnailFileName);

            // Must be the photo's name followed by a "-{width}x{height}" suffix.
            if (!thumbName.StartsWith(photoName + "-", StringComparison.Ordinal))
            {
                return false;
            }

            string suffix = thumbName.Substring(photoName.Length);
            return _thumbnailSuffix.IsMatch(suffix);
        }

        /// <summary>
        /// Produces the new name a thumbnail should take when its original photo
        /// is renamed, preserving the <c>-{width}x{height}{ext}</c> suffix.
        /// </summary>
        /// <param name="thumbnailFileName">The current thumbnail file name.</param>
        /// <param name="oldPhotoFileName">The original photo's current file name.</param>
        /// <param name="newPhotoFileName">The original photo's new file name.</param>
        /// <returns>The renamed thumbnail file name.</returns>
        public static string RenameThumbnail(string thumbnailFileName, string oldPhotoFileName, string newPhotoFileName)
        {
            string oldName = Path.GetFileNameWithoutExtension(oldPhotoFileName);
            string newName = Path.GetFileNameWithoutExtension(newPhotoFileName);

            // The thumbnail starts with the old photo name; swap just that prefix
            // so the "-{width}x{height}{ext}" tail is preserved verbatim.
            return newName + thumbnailFileName.Substring(oldName.Length);
        }
    }
}
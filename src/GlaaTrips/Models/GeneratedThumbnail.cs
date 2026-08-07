namespace GlaaTrips.Models
{
    /// <summary>
    /// A thumbnail produced by <see cref="ImageProcessor"/>: its file name (which
    /// carries the <c>{name}-{width}x{height}{ext}</c> convention) and its encoded
    /// bytes, ready to be persisted through an <see cref="IPhotoStore"/>.
    /// </summary>
    public sealed class GeneratedThumbnail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedThumbnail"/> class.
        /// </summary>
        /// <param name="fileName">The thumbnail file name.</param>
        /// <param name="content">The encoded thumbnail bytes.</param>
        public GeneratedThumbnail(string fileName, byte[] content)
        {
            FileName = fileName;
            Content = content;
        }

        /// <summary>Gets the thumbnail file name.</summary>
        public string FileName { get; }

        /// <summary>Gets the encoded thumbnail bytes.</summary>
        public byte[] Content { get; }
    }
}
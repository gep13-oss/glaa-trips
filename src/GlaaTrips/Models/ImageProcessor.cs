using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;

namespace GlaaTrips.Models
{
    public class ImageProcessor
    {
        private const int Quality = 75;

        /// <summary>
        /// Generates the thumbnail set for an image. The processor is storage
        /// agnostic: it decodes the source, produces one resized image per
        /// <see cref="ImageType"/> and returns each as named bytes for the caller
        /// to persist through an <see cref="IPhotoStore"/>. It performs no I/O of
        /// its own.
        /// </summary>
        /// <param name="imageStream">A readable stream over the source image bytes.</param>
        /// <param name="fileName">The original photo's file name, used to derive the thumbnail names and format.</param>
        /// <returns>The generated thumbnails; an empty list when the stream does not
        /// contain an image SkiaSharp can decode.</returns>
        public IReadOnlyList<GeneratedThumbnail> CreateThumbnails(Stream imageStream, string fileName)
        {
            string displayName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            var format = GetFormat(fileName);

            var thumbnails = new List<GeneratedThumbnail>();

            using (var inputStream = new SKManagedStream(imageStream))
            using (var codec = SKCodec.Create(inputStream))
            {
                // SKCodec/SKBitmap return null when the bytes are not a decodable
                // image — a file with an image extension but non-image or corrupt
                // content, or a format SkiaSharp cannot handle. Bail out instead of
                // dereferencing null (which surfaced as a 500 on upload).
                if (codec == null)
                {
                    return thumbnails;
                }

                using (var original = SKBitmap.Decode(codec))
                {
                    if (original == null)
                    {
                        return thumbnails;
                    }

                    using (var image = HandleOrientation(original, codec.EncodedOrigin))
                    {
                        foreach (ImageType type in Enum.GetValues(typeof(ImageType)))
                        {
                            int width = (int)type;
                            int height = (int)Math.Round(width * ((float)image.Height / image.Width));

                            var info = new SKImageInfo(width, height);

                            using (var resized = image.Resize(info, SKFilterQuality.High))
                            using (var thumb = SKImage.FromBitmap(resized))
                            using (var data = thumb.Encode(format, Quality))
                            {
                                thumbnails.Add(new GeneratedThumbnail($"{displayName}-{width}x{height}{ext}", data.ToArray()));
                            }
                        }
                    }
                }
            }

            return thumbnails;
        }

        private static SKEncodedImageFormat GetFormat(string fileName)
        {
            string ext = Path.GetExtension(fileName.ToLowerInvariant());

            switch (ext)
            {
                case ".gif":
                    return SKEncodedImageFormat.Gif;
                case ".png":
                    return SKEncodedImageFormat.Png;
                case ".webp":
                    return SKEncodedImageFormat.Webp;
            }

            return SKEncodedImageFormat.Jpeg;
        }

        // Got the code from https://stackoverflow.com/a/45620498/1074470
        private static SKBitmap HandleOrientation(SKBitmap bitmap, SKEncodedOrigin orientation)
        {
            SKBitmap rotated;
            switch (orientation)
            {
                case SKEncodedOrigin.BottomRight:

                    using (var surface = new SKCanvas(bitmap))
                    {
                        surface.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
                        surface.DrawBitmap(bitmap.Copy(), 0, 0);
                    }

                    return bitmap;

                case SKEncodedOrigin.RightTop:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(rotated.Width, 0);
                        surface.RotateDegrees(90);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                case SKEncodedOrigin.LeftBottom:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(0, rotated.Height);
                        surface.RotateDegrees(270);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                default:
                    return bitmap;
            }
        }
    }
}
using System;
using System.IO;
using SkiaSharp;

namespace GlaaTrips.Models
{
    public class ImageProcessor
    {
        private const int Quality = 75;

        /// <summary>
        /// Generates the thumbnail set for a saved image.
        /// </summary>
        /// <param name="imageStream">A readable stream over the source image bytes.</param>
        /// <param name="filePath">The saved image whose thumbnails are produced.</param>
        /// <returns><c>true</c> when thumbnails were generated; <c>false</c> when the
        /// stream does not contain an image SkiaSharp can decode.</returns>
        public bool CreateThumbnails(Stream imageStream, string filePath)
        {
            string dir = Path.Combine(Path.GetDirectoryName(filePath), "thumbnail");
            string displayName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);

            var format = GetFormat(filePath);

            using (var inputStream = new SKManagedStream(imageStream))
            using (var codec = SKCodec.Create(inputStream))
            {
                // SKCodec/SKBitmap return null when the bytes are not a decodable
                // image — a file with an image extension but non-image or corrupt
                // content, or a format SkiaSharp cannot handle. Bail out instead of
                // dereferencing null (which surfaced as a 500 on upload).
                if (codec == null)
                {
                    return false;
                }

                using (var original = SKBitmap.Decode(codec))
                {
                    if (original == null)
                    {
                        return false;
                    }

                    Directory.CreateDirectory(dir);

                    using (var image = HandleOrientation(original, codec.EncodedOrigin))
                    {
                        foreach (ImageType type in Enum.GetValues(typeof(ImageType)))
                        {
                            int width = (int)type;
                            int height = (int)Math.Round(width * ((float)image.Height / image.Width));

                            string thumbnailPath = Path.Combine(dir, $"{displayName}-{width}x{height}{ext}");
                            var info = new SKImageInfo(width, height);

                            using (var resized = image.Resize(info, SKFilterQuality.High))
                            using (var thumb = SKImage.FromBitmap(resized))
                            using (var fs = new FileStream(thumbnailPath, FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                thumb.Encode(format, Quality)
                                     .SaveTo(fs);
                            }
                        }
                    }
                }
            }

            return true;
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
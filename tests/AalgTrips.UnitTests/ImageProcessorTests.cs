using System.Text;
using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Coverage for <see cref="ImageProcessor.CreateThumbnails"/>, in particular
    /// its handling of content it cannot decode. SkiaSharp returns null from
    /// SKCodec/SKBitmap for non-image or unsupported bytes; the processor must
    /// report that as an empty result rather than dereference null, which
    /// previously surfaced as a 500 when such a file was uploaded. The processor
    /// is pure — it returns the generated thumbnails as named bytes and performs
    /// no I/O — so these tests assert on the returned set directly.
    /// </summary>
    [TestFixture]
    public class ImageProcessorTests
    {
        // An 8x8 PNG produced by SkiaSharp, so its own decoder accepts it.
        private const string RealPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=";

        [Test]
        public void CreateThumbnails_returns_named_bytes_for_a_real_image()
        {
            var thumbnails = Generate("shot.png", Convert.FromBase64String(RealPngBase64));

            Assert.That(thumbnails, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(thumbnails.All(t => t.Content.Length > 0), Is.True, "every thumbnail should carry bytes");
                Assert.That(
                    thumbnails.Select(t => t.FileName),
                    Has.All.Match(@"^shot-[0-9]+x[0-9]+\.png$"),
                    "thumbnail names follow the {name}-{width}x{height}{ext} convention");
            });
        }

        [Test]
        public void CreateThumbnails_returns_empty_for_non_image_content()
        {
            var thumbnails = Generate("not-really.png", Encoding.UTF8.GetBytes("this is plainly not an image"));

            Assert.That(thumbnails, Is.Empty, "non-image bytes must be reported, not throw");
        }

        // A 1x1 PNG is a valid file but decodes to null in SkiaSharp — the real case
        // that used to NRE the whole upload.
        [Test]
        public void CreateThumbnails_returns_empty_for_an_image_skiasharp_cannot_decode()
        {
            var onePixel = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

            var thumbnails = Generate("tiny.png", onePixel);

            Assert.That(thumbnails, Is.Empty);
        }

        private static IReadOnlyList<GeneratedThumbnail> Generate(string fileName, byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            return new ImageProcessor().CreateThumbnails(stream, fileName);
        }
    }
}
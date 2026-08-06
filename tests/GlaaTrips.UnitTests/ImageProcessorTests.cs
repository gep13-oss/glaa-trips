using System;
using System.IO;
using System.Text;
using GlaaTrips.Models;

namespace GlaaTrips.UnitTests
{
    /// <summary>
    /// Coverage for <see cref="ImageProcessor.CreateThumbnails"/>, in particular
    /// its handling of content it cannot decode. SkiaSharp returns null from
    /// SKCodec/SKBitmap for non-image or unsupported bytes; the processor must
    /// report that as <c>false</c> rather than dereference null, which previously
    /// surfaced as a 500 when such a file was uploaded.
    /// </summary>
    [TestFixture]
    public class ImageProcessorTests
    {
        // An 8x8 PNG produced by SkiaSharp, so its own decoder accepts it.
        private const string RealPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=";

        private string _dir = string.Empty;

        [SetUp]
        public void CreateDir()
        {
            _dir = Path.Combine(Path.GetTempPath(), "glaa-img-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void DeleteDir()
        {
            try
            {
                if (Directory.Exists(_dir))
                {
                    Directory.Delete(_dir, recursive: true);
                }
            }
            catch
            { /* best effort */
            }
        }

        [Test]
        public void CreateThumbnails_returns_true_and_writes_thumbnails_for_a_real_image()
        {
            var filePath = Save("shot.png", Convert.FromBase64String(RealPngBase64));

            bool created;
            using (var stream = File.OpenRead(filePath))
            {
                created = new ImageProcessor().CreateThumbnails(stream, filePath);
            }

            Assert.That(created, Is.True);
            Assert.That(Directory.GetFiles(Path.Combine(_dir, "thumbnail")), Is.Not.Empty);
        }

        [Test]
        public void CreateThumbnails_returns_false_for_non_image_content()
        {
            var filePath = Save("not-really.png", Encoding.UTF8.GetBytes("this is plainly not an image"));

            bool created;
            using (var stream = File.OpenRead(filePath))
            {
                created = new ImageProcessor().CreateThumbnails(stream, filePath);
            }

            Assert.That(created, Is.False, "non-image bytes must be reported, not throw");
            Assert.That(Directory.Exists(Path.Combine(_dir, "thumbnail")), Is.False, "no thumbnail folder should be created for undecodable content");
        }

        // A 1x1 PNG is a valid file but decodes to null in SkiaSharp — the real case
        // that used to NRE the whole upload.
        [Test]
        public void CreateThumbnails_returns_false_for_an_image_skiasharp_cannot_decode()
        {
            var onePixel = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
            var filePath = Save("tiny.png", onePixel);

            bool created;
            using (var stream = File.OpenRead(filePath))
            {
                created = new ImageProcessor().CreateThumbnails(stream, filePath);
            }

            Assert.That(created, Is.False);
        }

        private string Save(string name, byte[] bytes)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }
    }
}
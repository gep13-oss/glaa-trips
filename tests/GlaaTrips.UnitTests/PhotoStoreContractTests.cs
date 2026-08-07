using System.Text;
using GlaaTrips.Models;

namespace GlaaTrips.UnitTests
{
    /// <summary>
    /// The behaviour every <see cref="IPhotoStore"/> implementation must satisfy,
    /// run against each concrete store so the local disk store and the Azure Blob
    /// store are proven to behave identically. A derived fixture supplies a fresh,
    /// isolated store via <see cref="CreateStore"/>.
    /// </summary>
    public abstract class PhotoStoreContractTests
    {
        private const string Album = "sample-trip";

        /// <summary>
        /// Creates a fresh, isolated store for a single test.
        /// </summary>
        /// <returns>The store under test.</returns>
        protected abstract IPhotoStore CreateStore();

        [Test]
        public async Task Saved_photo_is_listed_readable_and_has_a_url()
        {
            var store = CreateStore();
            var bytes = Encoding.UTF8.GetBytes("original-photo-bytes");

            await store.SavePhotoAsync(Album, "beach.jpg", new MemoryStream(bytes));

            Assert.Multiple(() =>
            {
                Assert.That(store.PhotoExists(Album, "beach.jpg"), Is.True);
                Assert.That(store.ListPhotoFileNames(Album), Does.Contain("beach.jpg"));
                Assert.That(ReadAll(store.OpenPhoto(Album, "beach.jpg")), Is.EqualTo(bytes));
                Assert.That(store.PhotoUrl(Album, "beach.jpg"), Is.Not.Empty);
            });
        }

        [Test]
        public async Task Metadata_round_trips_and_the_album_is_listed()
        {
            var store = CreateStore();
            var metadata = new AlbumMetaData
            {
                DisplayName = "Sample Trip",
                Description = "A sample",
                Latitude = 55.95,
                Longitude = -3.19,
            };

            await store.WriteMetadataAsync(Album, metadata);

            var read = store.TryReadMetadata(Album);
            Assert.Multiple(() =>
            {
                Assert.That(store.AlbumExists(Album), Is.True);
                Assert.That(store.ListAlbumIds(), Does.Contain(Album));
                Assert.That(read, Is.Not.Null);
                Assert.That(read.DisplayName, Is.EqualTo("Sample Trip"));
                Assert.That(read.Latitude, Is.EqualTo(55.95));
                Assert.That(read.Longitude, Is.EqualTo(-3.19));
            });
        }

        [Test]
        public void Missing_metadata_reads_as_null()
        {
            var store = CreateStore();

            Assert.That(store.TryReadMetadata("no-such-album"), Is.Null);
        }

        [Test]
        public async Task Thumbnail_is_listed_and_has_a_url()
        {
            var store = CreateStore();

            await store.SaveThumbnailAsync(Album, "beach-190x127.jpg", new MemoryStream(Encoding.UTF8.GetBytes("thumb")));

            Assert.Multiple(() =>
            {
                Assert.That(store.ListThumbnailFileNames(Album), Does.Contain("beach-190x127.jpg"));
                Assert.That(store.ThumbnailUrl(Album, "beach-190x127.jpg"), Is.Not.Empty);
            });
        }

        [Test]
        public async Task Deleting_a_photo_removes_it_and_only_its_own_thumbnails()
        {
            var store = CreateStore();
            await store.SavePhotoAsync(Album, "beach.jpg", new MemoryStream(Encoding.UTF8.GetBytes("a")));
            await store.SaveThumbnailAsync(Album, "beach-190x127.jpg", new MemoryStream(Encoding.UTF8.GetBytes("t1")));
            await store.SaveThumbnailAsync(Album, "sunset-190x127.jpg", new MemoryStream(Encoding.UTF8.GetBytes("t2")));

            await store.DeletePhotoAsync(Album, "beach.jpg");

            Assert.Multiple(() =>
            {
                Assert.That(store.PhotoExists(Album, "beach.jpg"), Is.False);
                Assert.That(store.ListThumbnailFileNames(Album), Does.Not.Contain("beach-190x127.jpg"), "the photo's own thumbnail should go");
                Assert.That(store.ListThumbnailFileNames(Album), Does.Contain("sunset-190x127.jpg"), "another photo's thumbnail must be left alone");
            });
        }

        [Test]
        public async Task Renaming_a_photo_moves_it_and_its_thumbnails()
        {
            var store = CreateStore();
            await store.SavePhotoAsync(Album, "beach.jpg", new MemoryStream(Encoding.UTF8.GetBytes("a")));
            await store.SaveThumbnailAsync(Album, "beach-190x127.jpg", new MemoryStream(Encoding.UTF8.GetBytes("t")));

            await store.RenamePhotoAsync(Album, "beach.jpg", "shore.jpg");

            Assert.Multiple(() =>
            {
                Assert.That(store.PhotoExists(Album, "shore.jpg"), Is.True);
                Assert.That(store.PhotoExists(Album, "beach.jpg"), Is.False);
                Assert.That(store.ListThumbnailFileNames(Album), Does.Contain("shore-190x127.jpg"));
                Assert.That(store.ListThumbnailFileNames(Album), Does.Not.Contain("beach-190x127.jpg"));
            });
        }

        [Test]
        public async Task Deleting_an_album_removes_all_of_its_content()
        {
            var store = CreateStore();
            await store.WriteMetadataAsync(Album, new AlbumMetaData { DisplayName = "Sample" });
            await store.SavePhotoAsync(Album, "beach.jpg", new MemoryStream(Encoding.UTF8.GetBytes("a")));
            await store.SaveThumbnailAsync(Album, "beach-190x127.jpg", new MemoryStream(Encoding.UTF8.GetBytes("t")));

            await store.DeleteAlbumAsync(Album);

            Assert.Multiple(() =>
            {
                Assert.That(store.AlbumExists(Album), Is.False);
                Assert.That(store.ListPhotoFileNames(Album), Is.Empty);
                Assert.That(store.ListThumbnailFileNames(Album), Is.Empty);
                Assert.That(store.TryReadMetadata(Album), Is.Null);
            });
        }

        [Test]
        public async Task Writing_markers_succeeds_and_the_marker_url_is_set()
        {
            var store = CreateStore();

            await store.WriteMarkersAsync(new[] { new Marker { Lat = 55.95, Long = -3.19, Slug = Album } });

            Assert.That(store.MarkersUrl(), Is.Not.Empty);
        }

        private static byte[] ReadAll(Stream stream)
        {
            using (stream)
            {
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }
}
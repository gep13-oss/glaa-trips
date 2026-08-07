using GlaaTrips.Models;

namespace GlaaTrips.UnitTests
{
    /// <summary>
    /// Covers <see cref="Album.CoverPhoto"/>: the album's cover is the photo chosen
    /// through its metadata when one is set and still present, and otherwise falls
    /// back to the first photo (or none for an empty album).
    /// </summary>
    [TestFixture]
    public class AlbumCoverPhotoTests
    {
        [Test]
        public void Cover_defaults_to_the_first_photo_when_none_is_chosen()
        {
            var album = AlbumWith(cover: null, "a.jpg", "b.jpg", "c.jpg");

            Assert.That(album.CoverPhoto!.Id, Is.EqualTo("a.jpg"));
        }

        [Test]
        public void Cover_is_the_chosen_photo_when_one_is_set()
        {
            var album = AlbumWith(cover: "b.jpg", "a.jpg", "b.jpg", "c.jpg");

            Assert.That(album.CoverPhoto!.Id, Is.EqualTo("b.jpg"));
        }

        [Test]
        public void Cover_falls_back_to_the_first_photo_when_the_chosen_one_is_gone()
        {
            var album = AlbumWith(cover: "removed.jpg", "a.jpg", "b.jpg");

            Assert.That(album.CoverPhoto!.Id, Is.EqualTo("a.jpg"));
        }

        [Test]
        public void Cover_is_null_for_an_album_with_no_photos()
        {
            var album = AlbumWith(cover: null);

            Assert.That(album.CoverPhoto, Is.Null);
        }

        private static Album AlbumWith(string? cover, params string[] photoFileNames)
        {
            var album = new Album("trip", null, new AlbumMetaData { DisplayName = "Trip", CoverPhoto = cover });
            album.AddPhotos(photoFileNames.Select(f => new Photo(album, f)));
            return album;
        }
    }
}
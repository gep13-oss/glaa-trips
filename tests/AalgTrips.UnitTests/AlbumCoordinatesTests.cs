using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Covers <see cref="Album.Coordinates"/>, the map-reference string shown on the
    /// home trip cards and the album header. It must round to two decimals, use the
    /// correct hemisphere letter, and stay culture-invariant (a decimal point, never
    /// a comma) regardless of the server's locale.
    /// </summary>
    [TestFixture]
    public class AlbumCoordinatesTests
    {
        [TestCase(57.4183, -1.8618, "57.42°N 1.86°W")]
        [TestCase(55.9533, -3.1883, "55.95°N 3.19°W")]
        [TestCase(-33.8688, 151.2093, "33.87°S 151.21°E")]
        [TestCase(0.0, 0.0, "0.00°N 0.00°E")]
        public void Coordinates_formats_as_a_hemisphere_map_reference(double lat, double lng, string expected)
        {
            var album = new Album("/tmp/sample", null, new AlbumMetaData { Latitude = lat, Longitude = lng });

            Assert.That(album.Coordinates, Is.EqualTo(expected));
        }
    }
}
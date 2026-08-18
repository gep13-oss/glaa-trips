using System.Text.Json;
using AalgTrips.Models;

namespace AalgTrips.UnitTests
{
    /// <summary>
    /// Direct coverage for <see cref="CruiseCollection"/> over a
    /// <see cref="LocalDiskPhotoStore"/> pointed at a temp root: the copy-on-write
    /// mutations are visible and correctly ordered, a cruise reloads from the
    /// store, the generated route drops days at sea and keeps the ports in order,
    /// and cruises never leak into the album catalogue. Runs without a server; the
    /// UITests suite proves the same behaviour end-to-end.
    /// </summary>
    [TestFixture]
    public class CruiseCollectionTests : LocalStoreTestBase
    {
        [Test]
        public void Add_inserts_the_cruise_and_orders_newest_departure_first()
        {
            var cc = new CruiseCollection(Store());

            cc.Add(new Cruise("apple", Meta("Apple", new DateTime(2020, 5, 1))));
            cc.Add(new Cruise("zebra", Meta("Zebra", new DateTime(2026, 5, 1))));

            // Newest departure first, regardless of insertion order or name.
            Assert.That(cc.Cruises.Select(c => c.Id), Is.EqualTo(new[] { "zebra", "apple" }));
        }

        [Test]
        public void Cruises_are_ordered_newest_departure_first_then_by_id()
        {
            SeedCruiseOnDisk("older", "Older", new DateTime(2021, 1, 1));
            SeedCruiseOnDisk("newer", "Newer", new DateTime(2025, 1, 1));
            SeedCruiseOnDisk("same-b", "Same B", new DateTime(2023, 6, 1));
            SeedCruiseOnDisk("same-a", "Same A", new DateTime(2023, 6, 1));
            var cc = new CruiseCollection(Store());

            Assert.That(
                cc.Cruises.Select(c => c.Id),
                Is.EqualTo(new[] { "newer", "same-a", "same-b", "older" }),
                "newest departure first; cruises sharing a start date fall back to id order");
        }

        [Test]
        public void Remove_takes_the_matching_cruise_out_and_leaves_the_rest()
        {
            var cc = new CruiseCollection(Store());
            cc.Add(new Cruise("apple", Meta("Apple")));
            cc.Add(new Cruise("banana", Meta("Banana")));

            cc.Remove("APPLE");

            Assert.That(cc.Cruises.Select(c => c.Id), Is.EqualTo(new[] { "banana" }));
        }

        [Test]
        public void ReloadCruise_refreshes_the_metadata()
        {
            SeedCruiseOnDisk("trip", "Trip", new DateTime(2025, 1, 1));
            var cc = new CruiseCollection(Store());

            File.WriteAllText(
                Path.Combine(AlbumsRoot, "cruises", "trip", "cruise.json"),
                JsonSerializer.Serialize(Meta("Trip Renamed")));
            cc.ReloadCruise("trip");

            Assert.That(cc.Cruises.Single(c => c.Id == "trip").DisplayName, Is.EqualTo("Trip Renamed"));
        }

        [Test]
        public async Task WriteCruisesAsync_writes_ports_in_order_and_skips_days_at_sea()
        {
            SeedCruiseOnDisk("med", "Med Cruise", new DateTime(2025, 7, 27), new List<CruiseStop>
            {
                new CruiseStop { Date = new DateTime(2025, 7, 27), Name = "Rome", Depart = "17:00", Latitude = 42.09, Longitude = 11.80, Trips = new List<string> { "colosseum", "vatican-city" } },
                new CruiseStop { Date = new DateTime(2025, 7, 28), Name = "Cruising", AtSea = true },
                new CruiseStop { Date = new DateTime(2025, 7, 29), Name = "Santorini", Arrive = "13:00", Depart = "23:00", Latitude = 36.39, Longitude = 25.46 },
            });
            var cc = new CruiseCollection(Store());

            await cc.WriteCruisesAsync();

            var med = ReadRootJson<List<CruiseRoute>>(PhotoStoreConventions.CruisesFileName).Single();
            Assert.Multiple(() =>
            {
                Assert.That(med.Slug, Is.EqualTo("med"));
                Assert.That(med.Name, Is.EqualTo("Med Cruise"));

                // The day at sea is not a route vertex; the two ports are, in order.
                Assert.That(med.Ports.Select(p => p.Name), Is.EqualTo(new[] { "Rome", "Santorini" }));
                Assert.That(med.Ports[0].Lat, Is.EqualTo(42.09));
                Assert.That(med.Ports[0].Date, Is.EqualTo("27 Jul 2025"));
                Assert.That(med.Ports[0].Trips, Is.EqualTo(new[] { "colosseum", "vatican-city" }));
                Assert.That(med.Ports[1].Arrive, Is.EqualTo("13:00"));
            });
        }

        [Test]
        public void Cruises_are_kept_out_of_the_album_catalogue()
        {
            SeedAlbumOnDisk("edinburgh", "Edinburgh");
            SeedCruiseOnDisk("med", "Med Cruise", new DateTime(2025, 7, 27));

            var ac = new AlbumCollection(Store());
            var cc = new CruiseCollection(Store());

            Assert.Multiple(() =>
            {
                Assert.That(ac.Albums.Select(a => a.Id), Is.EqualTo(new[] { "edinburgh" }), "the cruise must not appear as an album");
                Assert.That(cc.Cruises.Select(c => c.Id), Is.EqualTo(new[] { "med" }));
            });
        }

        private static CruiseMetaData Meta(string displayName, DateTime? start = null)
        {
            var departed = start ?? new DateTime(2025, 1, 1);
            return new CruiseMetaData
            {
                DisplayName = displayName,
                Description = displayName + " description",
                StartDate = departed,
                EndDate = departed.AddDays(7),
            };
        }

        private void SeedCruiseOnDisk(string slug, string displayName, DateTime? start = null, List<CruiseStop>? stops = null)
        {
            var path = Path.Combine(AlbumsRoot, "cruises", slug);
            Directory.CreateDirectory(path);

            var departed = start ?? new DateTime(2025, 1, 1);
            var meta = new CruiseMetaData
            {
                DisplayName = displayName,
                Description = displayName + " description",
                StartDate = departed,
                EndDate = departed.AddDays(7),
                Stops = stops,
            };
            File.WriteAllText(Path.Combine(path, "cruise.json"), JsonSerializer.Serialize(meta));
        }
    }
}
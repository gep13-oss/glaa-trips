using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// Covers the Leaflet + OpenStreetMap album map that replaced Google Maps.
    /// OpenStreetMap tile requests are aborted so the suite stays hermetic and
    /// never depends on (or hammers) the public tile servers; the assertions ride
    /// on Leaflet's own DOM — the map container and the locally-served marker
    /// icons — and the marker-to-album navigation, none of which need tiles to
    /// paint.
    /// </summary>
    [TestFixture]
    public class MapTests : PageTest
    {
        [Test]
        public async Task Leaflet_initialises_the_map_container()
        {
            await BlockTilesAsync();
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");

            // Leaflet adds the leaflet-container class to the element it mounts on.
            await Expect(Page.Locator("#map.leaflet-container")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Each_album_with_coordinates_gets_a_marker()
        {
            await BlockTilesAsync();
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");

            // One seeded album with lat/long -> exactly one Leaflet marker icon.
            await Expect(Page.Locator(".leaflet-marker-icon")).ToHaveCountAsync(1);
        }

        [Test]
        public async Task Clicking_a_marker_navigates_to_its_album()
        {
            await BlockTilesAsync();
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");

            await Page.Locator(".leaflet-marker-icon").First.ClickAsync();

            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}"));
        }

        [Test]
        public async Task Leaflet_assets_are_served_locally()
        {
            var js = await Page.APIRequest.GetAsync(ServerFixture.BaseUrl + "/lib/leaflet/leaflet.js");
            var css = await Page.APIRequest.GetAsync(ServerFixture.BaseUrl + "/lib/leaflet/leaflet.css");

            Assert.That(js.Ok, Is.True, "leaflet.js should be served from wwwroot/lib");
            Assert.That(css.Ok, Is.True, "leaflet.css should be served from wwwroot/lib");
        }

        [Test]
        public async Task Home_page_uses_openstreetmap_not_google_maps()
        {
            var response = await Page.APIRequest.GetAsync(ServerFixture.BaseUrl + "/");
            var body = await response.TextAsync();

            Assert.That(body, Does.Not.Contain("maps.googleapis.com"), "the Google Maps script must be gone");
            Assert.That(body, Does.Contain("/lib/leaflet/leaflet.js"), "the page should load Leaflet");
        }

        private Task BlockTilesAsync()
        {
            return Page.RouteAsync("**/tile.openstreetmap.org/**", route => route.AbortAsync());
        }
    }
}
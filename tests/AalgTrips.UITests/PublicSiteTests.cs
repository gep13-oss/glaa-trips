using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Baseline coverage for viewing the site as a signed-in visitor: the album
    /// list, the map and an album page. The whole site now requires
    /// authentication (see <see cref="AuthenticationRequiredTests"/> for the
    /// anonymous-is-blocked behaviour), so each test signs in first.
    /// </summary>
    [TestFixture]
    public class PublicSiteTests : UITestBase
    {
        [SetUp]
        public async Task SignIn()
        {
            await SignInAsync();
        }

        [Test]
        public async Task Home_page_loads_and_lists_the_seeded_album()
        {
            await Page.GotoAsync(BaseUrl + "/");

            await Expect(Page).ToHaveTitleAsync(new Regex("GLAA Trips"));

            // The home page lists one trip card per album, each linking to the
            // album and showing its place name.
            var albumLink = Page.Locator($"a[href='/album/{ServerFixture.SampleAlbumSlug}/']");
            await Expect(albumLink).ToHaveCountAsync(1);
            await Expect(albumLink).ToContainTextAsync(ServerFixture.SampleAlbumTitle);
        }

        [Test]
        public async Task Home_page_renders_the_map_container()
        {
            await Page.GotoAsync(BaseUrl + "/");
            await Expect(Page.Locator("#map")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Markers_json_is_served_to_a_signed_in_visitor()
        {
            var response = await Page.APIRequest.GetAsync(BaseUrl + "/albums/markers.json");

            Assert.That(response.Ok, Is.True);
            var body = await response.TextAsync();
            Assert.That(body, Does.Contain(ServerFixture.SampleAlbumSlug));
        }

        [Test]
        public async Task Album_page_shows_the_album_title()
        {
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = ServerFixture.SampleAlbumTitle }))
                .ToBeVisibleAsync();
        }

        [Test]
        public async Task Unknown_album_returns_not_found()
        {
            var response = await Page.APIRequest.GetAsync($"{BaseUrl}/album/does-not-exist/");
            Assert.That(response.Status, Is.EqualTo(404));
        }
    }
}
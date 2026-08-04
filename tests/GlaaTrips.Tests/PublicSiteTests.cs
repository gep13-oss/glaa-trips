using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.Tests
{
    /// <summary>
    /// Baseline coverage for the public (anonymous) site: viewing albums and the
    /// map. These lock the read-side behaviour we intend to keep, so the later
    /// refactors (security, data layer, gallery, map swap) have a safety net.
    /// </summary>
    [TestFixture]
    public class PublicSiteTests : PageTest
    {
        [Test]
        public async Task Home_page_loads_and_lists_the_seeded_album()
        {
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");

            await Expect(Page).ToHaveTitleAsync(new Regex("GLAA Trips"));

            // The home page lists one anchor per album. (A photo-less album renders
            // an empty, zero-height tile, so assert presence + title rather than
            // visibility — cover-image rendering is covered by the gallery step.)
            var albumLink = Page.Locator($"a[href='/album/{ServerFixture.SampleAlbumSlug}/']");
            await Expect(albumLink).ToHaveCountAsync(1);
            await Expect(albumLink).ToHaveAttributeAsync("data-text", new Regex(ServerFixture.SampleAlbumTitle));
        }

        [Test]
        public async Task Home_page_renders_the_map_container()
        {
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");
            await Expect(Page.Locator("#map")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Markers_json_is_served_with_the_album_marker()
        {
            var response = await Page.APIRequest.GetAsync(ServerFixture.BaseUrl + "/albums/markers.json");

            Assert.That(response.Ok, Is.True);
            var body = await response.TextAsync();
            Assert.That(body, Does.Contain(ServerFixture.SampleAlbumSlug));
        }

        [Test]
        public async Task Album_page_shows_the_album_title()
        {
            await Page.GotoAsync($"{ServerFixture.BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = ServerFixture.SampleAlbumTitle }))
                .ToBeVisibleAsync();
        }

        [Test]
        public async Task Unknown_album_returns_not_found()
        {
            var response = await Page.APIRequest.GetAsync($"{ServerFixture.BaseUrl}/album/does-not-exist/");
            Assert.That(response.Status, Is.EqualTo(404));
        }
    }
}
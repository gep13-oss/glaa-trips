using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// End-to-end coverage for editing an album. The original handler read the
    /// slug from the request path with a "/Album/" regex that never matched the
    /// lower-case route, so every edit fell through to a 400; it also rebuilt the
    /// album from the slug (dropping its photos) and never rewrote markers.json.
    /// These tests lock in the fix: an authenticated admin can edit an album's
    /// metadata, and moving its coordinates updates the map's markers.json.
    /// </summary>
    [TestFixture]
    public class AlbumEditTests : UITestBase
    {
        private static string AlbumUrl => $"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/";

        [Test]
        public async Task Authenticated_admin_can_edit_album_metadata()
        {
            await SignInAsync();
            await Page.GotoAsync(AlbumUrl);

            var newDescription = "Edited by AlbumEditTests " + System.Guid.NewGuid().ToString("N");
            await Page.FillAsync("#admin #description", newDescription);
            await Page.ClickAsync("#btnEdit");

            // A successful edit redirects back to the album page (by slug). The old
            // broken handler returned 400 here, so reaching this URL is itself the
            // proof the regex bug is fixed.
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            var content = await Page.ContentAsync();
            Assert.That(content, Does.Contain(newDescription), "the album page should show the edited description");
        }

        [Test]
        public async Task Editing_album_coordinates_updates_markers_json()
        {
            await SignInAsync();
            await Page.GotoAsync(AlbumUrl);

            await Page.FillAsync("#admin #latitude", "12.34");
            await Page.FillAsync("#admin #longitude", "56.78");
            await Page.ClickAsync("#btnEdit");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            // Cache-bust the long-lived static file so we read the freshly written copy.
            var response = await Page.APIRequest.GetAsync($"{BaseUrl}/albums/markers.json?nocache={System.Guid.NewGuid():N}");
            Assert.That(response.Status, Is.EqualTo(200));

            var markers = JsonSerializer.Deserialize<List<MarkerDto>>(
                await response.TextAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var marker = markers.SingleOrDefault(m => m.Slug == ServerFixture.SampleAlbumSlug);
            Assert.That(marker, Is.Not.Null, "the sample album should still have a marker");
            Assert.Multiple(() =>
            {
                Assert.That(marker!.Lat, Is.EqualTo(12.34), "latitude should reflect the edit");
                Assert.That(marker!.Long, Is.EqualTo(56.78), "longitude should reflect the edit");
            });
        }

        private sealed class MarkerDto
        {
            public double Lat { get; set; }

            public double Long { get; set; }

            public string Slug { get; set; } = string.Empty;
        }
    }
}
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AalgTrips.UITests
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
            await OpenAlbumActionAsync("editDialog");
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

            await OpenAlbumActionAsync("editDialog");
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

        [Test]
        public async Task Creating_an_album_whose_name_collides_with_an_existing_slug_is_rejected()
        {
            await SignInAsync();
            await Page.GotoAsync(BaseUrl + "/");

            // The seeded album already owns the "sample-trip" slug. A new trip whose
            // title slugs to the same value must be refused, not written over the
            // existing album (which would clobber its metadata and duplicate it).
            await OpenAddTripModalAsync();
            await Page.FillAsync("#name", ServerFixture.SampleAlbumTitle);
            await Page.FillAsync("#visited", "2019-03-03");
            await Page.FillAsync("#latitude", "10");
            await Page.FillAsync("#longitude", "20");

            var response = await Page.RunAndWaitForResponseAsync(
                () => Page.ClickAsync("#newalbum"),
                r => r.Request.Method == "POST" && r.Url.Contains("/album/new/create"));

            Assert.That(response.Status, Is.EqualTo(409), "a duplicate-slug create should be rejected with 409 Conflict");

            // The original album must still be present and reachable — proof the
            // rejected create did not overwrite it.
            var existing = await Page.APIRequest.GetAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            Assert.That(existing.Status, Is.EqualTo(200), "the existing album should be untouched by the rejected create");
        }

        [Test]
        public async Task Renaming_an_album_changes_its_slug_url_and_marker()
        {
            await SignInAsync();

            try
            {
                await Page.GotoAsync(AlbumUrl);
                await OpenAlbumActionAsync("renameDialog");
                await Page.FillAsync("#renameName", "Renamed Sample Trip");
                await Page.ClickAsync("#btnRename");

                // The rename redirects to the album under its new slug.
                await Page.WaitForURLAsync(new Regex("/album/renamed-sample-trip/$"));

                var oldUrl = await Page.APIRequest.GetAsync(AlbumUrl);
                var newUrl = await Page.APIRequest.GetAsync($"{BaseUrl}/album/renamed-sample-trip/");
                Assert.Multiple(() =>
                {
                    Assert.That(oldUrl.Status, Is.EqualTo(404), "the old slug should no longer resolve");
                    Assert.That(newUrl.Status, Is.EqualTo(200), "the album should be reachable under the new slug");
                });

                var markers = await ReadMarkersAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(markers.Any(m => m.Slug == "renamed-sample-trip"), Is.True, "the marker should follow the new slug");
                    Assert.That(markers.Any(m => m.Slug == ServerFixture.SampleAlbumSlug), Is.False, "the old slug's marker should be gone");
                });
            }
            finally
            {
                // Restore the sample album's slug so the rest of the suite is unaffected,
                // whether or not the assertions above passed.
                await RestoreSampleSlugAsync();
            }
        }

        private async Task RestoreSampleSlugAsync()
        {
            var renamed = await Page.APIRequest.GetAsync($"{BaseUrl}/album/renamed-sample-trip/");
            if (renamed.Status != 200)
            {
                return;
            }

            await Page.GotoAsync($"{BaseUrl}/album/renamed-sample-trip/");
            await OpenAlbumActionAsync("renameDialog");
            await Page.FillAsync("#renameName", ServerFixture.SampleAlbumTitle);
            await Page.ClickAsync("#btnRename");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));
        }

        private async Task<List<MarkerDto>> ReadMarkersAsync()
        {
            var response = await Page.APIRequest.GetAsync($"{BaseUrl}/albums/markers.json?nocache={System.Guid.NewGuid():N}");
            return JsonSerializer.Deserialize<List<MarkerDto>>(
                await response.TextAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private sealed class MarkerDto
        {
            public double Lat { get; set; }

            public double Long { get; set; }

            public string Slug { get; set; } = string.Empty;
        }
    }
}
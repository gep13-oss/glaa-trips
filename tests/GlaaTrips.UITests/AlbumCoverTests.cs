using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// End-to-end coverage for choosing an album's cover (feature) photo. By
    /// default the cover is the first photo; an admin can pick any photo in the
    /// album as the cover, and that choice is what the home page trip card shows.
    /// The test uploads two photos, verifies the default, switches the cover, and
    /// checks both the album page indicator and the home-page card, then cleans up.
    /// </summary>
    [TestFixture]
    public class AlbumCoverTests : UITestBase
    {
        private static readonly byte[] SamplePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=");

        [Test]
        public async Task Choosing_a_cover_photo_changes_the_home_page_card()
        {
            await SignInAsync();

            // "alpha" sorts before "bravo", so it is the default cover.
            await UploadAsync("alpha.png");
            await UploadAsync("bravo.png");

            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            // Default cover is the first photo.
            await Expect(Page.Locator(".thumb--cover")).ToHaveCountAsync(1);
            await Expect(Page.Locator(".thumb--cover a")).ToHaveAttributeAsync("data-text", "alpha");

            // Choose bravo as the cover.
            await ThumbFor("bravo").Locator(".thumb__cover-btn").ClickAsync();
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            // The album page now marks bravo as the cover.
            await Expect(Page.Locator(".thumb--cover a")).ToHaveAttributeAsync("data-text", "bravo");

            // And the home page trip card uses bravo's image as the cover.
            await Page.GotoAsync(BaseUrl + "/");
            var coverSrc = await Page.GetAttributeAsync(
                $"a[href='/album/{ServerFixture.SampleAlbumSlug}/'] img", "src");
            Assert.That(coverSrc, Does.Contain("bravo"), "the home-page cover should be the chosen photo");

            await DeletePhotoAsync("alpha");
            await DeletePhotoAsync("bravo");
        }

        private ILocator ThumbFor(string dataText)
        {
            return Page.Locator(".thumb").Filter(new LocatorFilterOptions
            {
                Has = Page.Locator($"a[data-text='{dataText}']"),
            });
        }

        private async Task UploadAsync(string name)
        {
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = name,
                MimeType = "image/png",
                Buffer = SamplePng,
            });
            await Page.ClickAsync("#btnfiles");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));
        }

        private async Task DeletePhotoAsync(string displayName)
        {
            var token = await AntiforgeryTokenAsync($"/album/{ServerFixture.SampleAlbumSlug}/");
            await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/{displayName}/delete",
                FormPost(token));
        }
    }
}
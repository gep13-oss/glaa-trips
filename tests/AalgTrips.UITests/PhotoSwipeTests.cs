using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Covers the PhotoSwipe lightbox on the album page. The gallery is a
    /// progressive enhancement: each thumbnail's anchor still points at the
    /// per-photo page, and gallery.js opens the lightbox on click using the
    /// full-size image from the data-pswp-* attributes. The seeded album has no
    /// photos, so the test uploads a real one (reusing the upload flow that
    /// generates the thumbnail set), exercises the lightbox, then deletes it so the
    /// shared sample album is left as it was found.
    /// </summary>
    [TestFixture]
    public class PhotoSwipeTests : UITestBase
    {
        // An 8x8 PNG produced by SkiaSharp so the upload handler re-decodes it
        // cleanly and generates the full thumbnail set (matches AlbumUploadTests).
        private static readonly byte[] SamplePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=");

        [Test]
        public async Task Clicking_a_thumbnail_opens_and_closes_the_lightbox()
        {
            var photoLink = await UploadSamplePhotoAsync("gallery-shot.png");

            try
            {
                await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

                // Progressive enhancement: the anchor still points at the per-photo
                // page (the no-JS / crawler / admin route) and carries the large
                // image for the lightbox in data-pswp-src.
                var thumb = Page.Locator($"#gallery a[href='{photoLink}']");
                await Expect(thumb).ToHaveAttributeAsync("data-pswp-src", new Regex("/thumbnail/gallery-shot-960x"));

                // Wait until gallery.js has bound the lightbox, otherwise a click
                // could follow the href to the photo page instead of opening it.
                await Expect(Page.Locator("#gallery[data-pswp-ready]")).ToHaveCountAsync(1);

                await thumb.ClickAsync();

                // The lightbox opens and shows the full-size image. PhotoSwipe also
                // renders a blur-up placeholder from the small thumbnail, so target
                // the real slide image specifically (not the --placeholder one).
                await Expect(Page.Locator(".pswp")).ToBeVisibleAsync();
                await Expect(Page.Locator(".pswp img.pswp__img:not(.pswp__img--placeholder)"))
                    .ToHaveAttributeAsync("src", new Regex("gallery-shot-960x"));

                // Escape closes it; PhotoSwipe removes the .pswp element from the DOM.
                await Page.Keyboard.PressAsync("Escape");
                await Expect(Page.Locator(".pswp")).ToHaveCountAsync(0);

                // We never navigated away from the album page.
                await Expect(Page).ToHaveURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));
            }
            finally
            {
                await DeletePhotoAsync(photoLink);
            }
        }

        [Test]
        public async Task Photoswipe_assets_are_served_locally()
        {
            var css = await Page.APIRequest.GetAsync(BaseUrl + "/lib/photoswipe/photoswipe.css");
            var core = await Page.APIRequest.GetAsync(BaseUrl + "/lib/photoswipe/photoswipe.esm.min.js");
            var lightbox = await Page.APIRequest.GetAsync(BaseUrl + "/lib/photoswipe/photoswipe-lightbox.esm.min.js");

            Assert.That(css.Ok, Is.True, "photoswipe.css should be served from wwwroot/lib");
            Assert.That(core.Ok, Is.True, "photoswipe.esm.min.js should be served from wwwroot/lib");
            Assert.That(lightbox.Ok, Is.True, "photoswipe-lightbox.esm.min.js should be served from wwwroot/lib");
        }

        [Test]
        public async Task Arrow_keys_stay_in_the_lightbox_instead_of_jumping_to_another_album()
        {
            await SignInAsync();

            // Give the sample album a neighbour so its prev/next-album link — the one
            // the old global key handler followed — is actually rendered in the pager.
            var token = await AntiforgeryTokenAsync("/");
            await Page.APIRequest.PostAsync(
                $"{BaseUrl}/album/new/create/",
                FormPost(token, ("name", "Arrow Neighbour"), ("visited", "2025-01-01"), ("latitude", "10"), ("longitude", "20")));

            var photoLink = await UploadSamplePhotoAsync("arrow-shot.png");

            try
            {
                await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

                // The pager now renders an adjacent-album link: proof the old hijack could fire.
                await Expect(Page.Locator("a[rel=next], a[rel=prev]").First).ToBeVisibleAsync();
                await Expect(Page.Locator("#gallery[data-pswp-ready]")).ToHaveCountAsync(1);

                await Page.Locator($"#gallery a[href='{photoLink}']").ClickAsync();
                await Expect(Page.Locator(".pswp")).ToBeVisibleAsync();

                await Page.Keyboard.PressAsync("ArrowRight");
                await Page.Keyboard.PressAsync("ArrowLeft");

                // The arrows stayed inside the lightbox rather than following the album
                // link, so we are still on the same album with the lightbox open.
                await Expect(Page.Locator(".pswp")).ToBeVisibleAsync();
                await Expect(Page).ToHaveURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/"));
            }
            finally
            {
                await DeletePhotoAsync(photoLink);
                var deleteToken = await AntiforgeryTokenAsync("/");
                await Page.APIRequest.PostAsync($"{BaseUrl}/album/arrow-neighbour/delete", FormPost(deleteToken));
            }
        }

        private async Task<string> UploadSamplePhotoAsync(string fileName)
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await OpenAlbumActionAsync("uploadDialog");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = fileName,
                MimeType = "image/png",
                Buffer = SamplePng,
            });
            await Page.ClickAsync("#btnfiles");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            var displayName = Path.GetFileNameWithoutExtension(fileName);
            return $"/photo/{ServerFixture.SampleAlbumSlug}/{displayName}/";
        }

        private async Task DeletePhotoAsync(string photoLink)
        {
            var token = await AntiforgeryTokenAsync();
            await Page.APIRequest.PostAsync($"{BaseUrl}{photoLink}delete", FormPost(token));
        }
    }
}
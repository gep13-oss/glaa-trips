using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Covers the PhotoSwipe deep-linking and share button. Opening a photo in the
    /// lightbox reflects it in the URL fragment (<c>#photo=&lt;name&gt;</c>), the
    /// share button copies that link (and records it on the gallery for the test),
    /// and loading the album with such a fragment opens the lightbox straight to
    /// that photo. A real photo is uploaded for the run and removed afterwards.
    /// </summary>
    [TestFixture]
    public class PhotoSwipeShareTests : UITestBase
    {
        private static readonly byte[] SamplePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=");

        [SetUp]
        public async Task UploadPhoto()
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = "shareable.png",
                MimeType = "image/png",
                Buffer = SamplePng,
            });
            await Page.ClickAsync("#btnfiles");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));
        }

        [TearDown]
        public async Task DeletePhoto()
        {
            var token = await AntiforgeryTokenAsync($"/album/{ServerFixture.SampleAlbumSlug}/");
            await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/shareable/delete",
                FormPost(token));
        }

        [Test]
        public async Task Opening_a_photo_deep_links_it_and_the_share_button_copies_the_link()
        {
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            await Expect(Page.Locator("#gallery[data-pswp-ready]")).ToHaveCountAsync(1);

            // Open the lightbox on the photo.
            await Page.Locator("#gallery a[data-pswp-src]").First.ClickAsync();
            await Expect(Page.Locator(".pswp")).ToBeVisibleAsync();

            // The open photo is reflected in the URL fragment...
            await Expect(Page).ToHaveURLAsync(new Regex("#photo=shareable"));

            // ...and the share button records the shareable deep link.
            await Page.Locator(".pswp button[title='Copy link to this photo']").ClickAsync();
            var shared = await Page.GetAttributeAsync("#gallery", "data-pswp-share-url");
            Assert.That(shared, Does.EndWith("#photo=shareable"));
        }

        [Test]
        public async Task A_deep_link_opens_the_lightbox_on_that_photo()
        {
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/#photo=shareable");

            // The lightbox opens by itself, straight to the linked photo.
            await Expect(Page.Locator(".pswp")).ToBeVisibleAsync();
            await Expect(Page.Locator(".pswp__img").First).ToBeVisibleAsync();
        }
    }
}
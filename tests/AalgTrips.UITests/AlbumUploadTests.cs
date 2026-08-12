using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// End-to-end coverage for uploading a photo. This exercises the OnPostUpload
    /// path that saves the original and generates thumbnails. The handler now writes
    /// the original file first and derives thumbnails from the saved copy, so a
    /// decode failure can no longer leave an orphaned zero-byte image. The test
    /// uploads a real (tiny) PNG, confirms the photo appears with a generated,
    /// served thumbnail and a served original, then deletes it so the shared sample
    /// album is left as it was found (which also covers the photo-delete path).
    /// </summary>
    [TestFixture]
    public class AlbumUploadTests : UITestBase
    {
        // A small (8x8) PNG that SkiaSharp reliably decodes and resizes. It was
        // produced by SkiaSharp itself so the encoder that reads it back in the
        // upload handler is guaranteed to accept it. (A 1x1 PNG decodes to null in
        // SkiaSharp, which would surface as a separate handler robustness gap.)
        private static readonly byte[] SamplePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=");

        [Test]
        public async Task Uploading_a_photo_saves_the_original_and_generates_a_thumbnail()
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await OpenAlbumActionAsync("uploadDialog");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = "uploaded-shot.png",
                MimeType = "image/png",
                Buffer = SamplePng,
            });
            await Page.ClickAsync("#btnfiles");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            var photoLink = $"/photo/{ServerFixture.SampleAlbumSlug}/uploaded-shot/";

            // The uploaded photo now appears on the album page...
            await Expect(Page.Locator($"a[href='{photoLink}']")).ToHaveCountAsync(1);

            // ...with a generated thumbnail that is actually served.
            var thumbSrc = await Page.GetAttributeAsync($"a[href='{photoLink}'] img", "src");
            Assert.That(thumbSrc, Does.Contain("/thumbnail/uploaded-shot-"), "a thumbnail should have been generated");
            var thumb = await Page.APIRequest.GetAsync(BaseUrl + thumbSrc);
            Assert.That(thumb.Ok, Is.True, "the generated thumbnail should be served");

            // ...and the original image is saved and served.
            var original = await Page.APIRequest.GetAsync($"{BaseUrl}/albums/{ServerFixture.SampleAlbumSlug}/uploaded-shot.png");
            Assert.That(original.Ok, Is.True, "the original image should be saved and served");

            // Clean up so the shared sample album is left photo-less, and exercise
            // the real photo-delete path (removes the original and its thumbnails).
            var token = await AntiforgeryTokenAsync();
            var delete = await Page.APIRequest.PostAsync(
                $"{BaseUrl}{photoLink}delete",
                FormPost(token));
            Assert.That(delete.Status, Is.EqualTo(302), "the cleanup delete should redirect back to the album");
        }

        [Test]
        public async Task Uploading_a_file_that_is_not_a_real_image_is_skipped_without_error()
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await OpenAlbumActionAsync("uploadDialog");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = "bad-shot.png",
                MimeType = "image/png",
                Buffer = System.Text.Encoding.UTF8.GetBytes("this is not a real image"),
            });
            await Page.ClickAsync("#btnfiles");

            // The upload still completes (redirects back to the album) instead of
            // 500ing on the undecodable content...
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            // ...and the bad file is skipped, so no photo is added for it.
            await Expect(Page.Locator($"a[href='/photo/{ServerFixture.SampleAlbumSlug}/bad-shot/']"))
                .ToHaveCountAsync(0);
        }

        [Test]
        public async Task An_upload_larger_than_the_default_request_limit_is_accepted()
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            // ~31 MB — above Kestrel's 30 MB default request-body limit, which a
            // batch of phone photos easily exceeds. The bytes are not a real image
            // so the file is skipped; the point is the request is ACCEPTED (the
            // handler runs and redirects) rather than rejected 400 for a body that
            // is too large to even read (which was the production failure).
            var oversize = new byte[31 * 1024 * 1024];

            await OpenAlbumActionAsync("uploadDialog");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = "oversize.png",
                MimeType = "image/png",
                Buffer = oversize,
            });
            await Page.ClickAsync("#btnfiles");

            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));
            await Expect(Page.Locator($"a[href='/photo/{ServerFixture.SampleAlbumSlug}/oversize/']"))
                .ToHaveCountAsync(0);
        }
    }
}
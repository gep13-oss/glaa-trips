using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Covers deleting a photo directly from the album grid. Each thumbnail carries
    /// a small admin-only delete form (shown on hover) that posts to the photo's
    /// delete handler and returns to the album, so an admin no longer has to open
    /// the per-photo page to remove a shot. The test uploads a real photo, deletes
    /// it through the grid control, and confirms it is gone.
    /// </summary>
    [TestFixture]
    public class PhotoGridDeleteTests : UITestBase
    {
        // An 8x8 PNG SkiaSharp decodes cleanly (matches the other upload tests).
        private static readonly byte[] SamplePng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAABHNCSVQICAgIfAhkiAAAABZJREFUGJVjTJn69j8DHsCET3L4KAAA/T0C9UyjKGsAAAAASUVORK5CYII=");

        [Test]
        public async Task Admin_can_delete_a_photo_from_the_album_grid()
        {
            await SignInAsync();
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");

            await OpenAlbumActionAsync("uploadDialog");
            await Page.SetInputFilesAsync("#files", new FilePayload
            {
                Name = "grid-delete-shot.png",
                MimeType = "image/png",
                Buffer = SamplePng,
            });
            await Page.ClickAsync("#btnfiles");
            await Page.WaitForURLAsync(new Regex($"/album/{ServerFixture.SampleAlbumSlug}/$"));

            var photoLink = $"/photo/{ServerFixture.SampleAlbumSlug}/grid-delete-shot/";
            var thumb = Page.Locator($"#gallery .thumb:has(a[href='{photoLink}'])");
            await Expect(thumb).ToHaveCountAsync(1);

            // The grid delete asks for confirmation via admin.js; accept it.
            Page.Dialog += (_, dialog) => dialog.AcceptAsync();

            await thumb.Locator(".thumb__delete-btn").ClickAsync();

            // After the delete + redirect back to the album, the photo is gone.
            await Expect(Page.Locator($"#gallery a[href='{photoLink}']")).ToHaveCountAsync(0);
        }
    }
}
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Path-traversal coverage for the admin mutation endpoints. User-supplied
    /// values flow into <c>Path.Combine</c> in several handlers: the new photo name
    /// in <c>Photo.OnPostRename</c> (a form field) and the album name/slug in the
    /// album create/edit/delete handlers (route values). Without validation a value
    /// such as <c>../../evil</c> would move, overwrite, or recursively delete files
    /// outside the albums web root. These tests exercise the guard as an
    /// authenticated admin — so a 302 login challenge cannot mask a missing check —
    /// and confirm the request is rejected and nothing outside the album is touched.
    /// </summary>
    [TestFixture]
    public class PathTraversalTests : UITestBase
    {
        // Values that must never be accepted as a photo name / album segment.
        // Covers POSIX and Windows separators, the parent token on its own, and a
        // rooted path.
        private static readonly string[] MaliciousSegments =
        {
            "../evil",
            "..\\evil",
            "..",
            "sub/evil",
            "/etc/passwd",
        };

        [Test]
        public async Task Authenticated_rename_with_a_traversal_name_is_rejected(
            [ValueSource(nameof(MaliciousSegments))] string maliciousName)
        {
            await SignInAsync();
            var token = await AntiforgeryTokenAsync();

            // photoName is irrelevant: the guard rejects the malicious new name
            // before the photo is ever resolved.
            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/any-photo/rename",
                FormPost(token, ("name", maliciousName)));

            Assert.That(response.Status, Is.EqualTo(400), $"a traversal rename name ('{maliciousName}') must be rejected");
        }

        [Test]
        public async Task Authenticated_create_with_an_empty_slug_is_rejected()
        {
            await SignInAsync();
            var token = await AntiforgeryTokenAsync();

            // A title of pure punctuation slugs to an empty string, which would
            // otherwise resolve to the albums root directory itself.
            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/album/new/create/",
                FormPost(
                    token,
                    ("name", "***"),
                    ("description", "no slug"),
                    ("visited", "2026-01-01"),
                    ("latitude", "0"),
                    ("longitude", "0")));

            Assert.That(response.Status, Is.EqualTo(400), "a title that slugs to empty must be rejected, not written to the albums root");
        }

        [Test]
        public async Task A_traversal_album_delete_does_not_destroy_anything_outside_the_album(
            [Values("..%2F..%2Fsample-trip", "..%5C..%5Csample-trip", "..")] string encodedName)
        {
            await SignInAsync();
            var token = await AntiforgeryTokenAsync();

            // Whether the framework normalises the path or the server-side guard
            // rejects it, the outcome that matters is the same: the sample album
            // (and the web root around it) survive untouched.
            await Page.APIRequest.PostAsync(
                $"{BaseUrl}/album/{encodedName}/delete",
                FormPost(token));

            var survivor = await Page.APIRequest.GetAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            Assert.That(survivor.Status, Is.EqualTo(200), "a traversal delete must not remove the sample album");
        }
    }
}
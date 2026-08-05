using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.UITests
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
    public class PathTraversalTests : PageTest
    {
        private static string BaseUrl => ServerFixture.BaseUrl;

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
            await SignIn();
            var token = await AntiforgeryToken();

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
            await SignIn();
            var token = await AntiforgeryToken();

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
            await SignIn();
            var token = await AntiforgeryToken();

            // Whether the framework normalises the path or the server-side guard
            // rejects it, the outcome that matters is the same: the sample album
            // (and the web root around it) survive untouched.
            await Page.APIRequest.PostAsync(
                $"{BaseUrl}/album/{encodedName}/delete",
                FormPost(token, System.Array.Empty<(string, string)>()));

            var survivor = await Page.APIRequest.GetAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            Assert.That(survivor.Status, Is.EqualTo(200), "a traversal delete must not remove the sample album");
        }

        private static APIRequestContextOptions FormPost(string token, params (string Key, string Value)[] fields)
        {
            var body = "__RequestVerificationToken=" + Uri.EscapeDataString(token);

            foreach (var (key, value) in fields)
            {
                body += "&" + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
            }

            return new APIRequestContextOptions
            {
                MaxRedirects = 0,
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/x-www-form-urlencoded",
                },
                Data = body,
            };
        }

        private async Task SignIn()
        {
            await Page.GotoAsync(BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", ServerFixture.TestPassword);
            await Page.ClickAsync("input[type=submit]");
            await Page.WaitForURLAsync(BaseUrl + "/");
        }

        // After sign-in the home page renders the admin forms, each carrying a
        // valid authenticated antiforgery token. Page.APIRequest shares the
        // browser context cookie jar, so posts made with this token are both
        // authenticated and antiforgery-valid — isolating the path guard as the
        // only thing under test.
        private async Task<string> AntiforgeryToken()
        {
            await Page.GotoAsync(BaseUrl + "/");
            var token = await Page.GetAttributeAsync("input[name='__RequestVerificationToken']", "value");
            return token!;
        }
    }
}
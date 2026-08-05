using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.Tests
{
    /// <summary>
    /// Authorization coverage for the admin mutation endpoints. Before the
    /// security pass these handlers carried <c>[Authorize]</c> on the handler
    /// methods, which Razor Pages silently ignores (analyzer MVC1001), leaving
    /// album/photo create/edit/delete/upload/rename open to anonymous POSTs.
    /// These tests lock in the fix: every admin handler now challenges an
    /// unauthenticated request (redirect to /login) and performs no mutation,
    /// while an authenticated admin can still create and delete.
    /// </summary>
    [TestFixture]
    public class AdminAuthorizationTests : PageTest
    {
        private static string BaseUrl => ServerFixture.BaseUrl;

        // Every mutating admin endpoint. A real attacker can obtain an
        // (anonymous) antiforgery token, so exercising these with a valid token
        // proves the server-side authorization guard — not just antiforgery.
        [TestCase("/album/" + ServerFixture.SampleAlbumSlug + "/delete")]
        [TestCase("/album/" + ServerFixture.SampleAlbumSlug + "/edit")]
        [TestCase("/album/" + ServerFixture.SampleAlbumSlug + "/upload")]
        [TestCase("/album/new/create/")]
        [TestCase("/photo/" + ServerFixture.SampleAlbumSlug + "/any-photo/delete")]
        [TestCase("/photo/" + ServerFixture.SampleAlbumSlug + "/any-photo/rename")]
        public async Task Anonymous_post_to_admin_endpoint_is_challenged(string path)
        {
            var response = await PostAnonymouslyWithToken(path);

            Assert.That(response.Status, Is.EqualTo(302), "expected a login challenge, not the mutation");
            Assert.That(response.Headers.TryGetValue("location", out var location), Is.True);
            Assert.That(location, Does.Contain("/login"));
        }

        [Test]
        public async Task Anonymous_delete_leaves_the_album_in_place()
        {
            await PostAnonymouslyWithToken("/album/" + ServerFixture.SampleAlbumSlug + "/delete");

            var check = await Page.APIRequest.GetAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            Assert.That(check.Status, Is.EqualTo(200), "the album must survive an anonymous delete attempt");
        }

        [Test]
        public async Task Authenticated_admin_can_create_then_delete_an_album()
        {
            await SignIn();

            // Create a throwaway album via the home-page admin form (the form
            // and its antiforgery token are handled by the real browser).
            await Page.FillAsync("#admin #name", "Auth Test Album");
            await Page.FillAsync("#admin #description", "Created by AdminAuthorizationTests");
            await Page.FillAsync("#admin #visited", "2026-02-02");
            await Page.FillAsync("#admin #latitude", "10.0");
            await Page.FillAsync("#admin #longitude", "20.0");
            await Page.ClickAsync("#newalbum");

            await Page.WaitForURLAsync(new Regex("/album/[^/]+/$"));
            var albumUrl = Page.Url;

            var created = await Page.APIRequest.GetAsync(albumUrl);
            Assert.That(created.Status, Is.EqualTo(200), "the authenticated admin should be able to create an album");

            // ...and delete it again, so the test cleans up after itself.
            // admin.js guards the delete button with a confirm() dialog; accept
            // it so the form actually submits (Playwright dismisses by default).
            Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await Page.ClickAsync("#deletealbum");
            await Page.WaitForURLAsync(BaseUrl + "/");

            var afterDelete = await Page.APIRequest.GetAsync(albumUrl);
            Assert.That(afterDelete.Status, Is.EqualTo(404), "the album should be gone after the admin deletes it");
        }

        private async Task SignIn()
        {
            await Page.GotoAsync(BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", ServerFixture.TestPassword);
            await Page.ClickAsync("input[type=submit]");
            await Page.WaitForURLAsync(BaseUrl + "/");
        }

        private async Task<IAPIResponse> PostAnonymouslyWithToken(string path)
        {
            // Rendering the login page yields a valid antiforgery token + cookie
            // (Razor Pages validate antiforgery on every POST). Both are
            // anonymous-scoped — exactly what an unauthenticated attacker has.
            // Page.APIRequest shares the browser context's cookie jar, so the
            // matching cookie is sent with the POST below.
            await Page.GotoAsync(BaseUrl + "/login");
            var token = await Page.GetAttributeAsync("input[name='__RequestVerificationToken']", "value");

            return await Page.APIRequest.PostAsync(BaseUrl + path, new APIRequestContextOptions
            {
                MaxRedirects = 0,
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/x-www-form-urlencoded",
                },
                Data = "__RequestVerificationToken=" + Uri.EscapeDataString(token!),
            });
        }
    }
}
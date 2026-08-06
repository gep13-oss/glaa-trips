using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// Shared base for the Playwright UI tests. Centralises the pieces the
    /// admin-facing tests all need: the server base URL, the sign-in flow, and the
    /// antiforgery-token / form-POST helpers used to drive the mutation endpoints
    /// through <c>Page.APIRequest</c>. Tests that only read the public site
    /// (for example the login and public-site suites) can extend
    /// <see cref="PageTest"/> directly instead.
    /// </summary>
    public abstract class UITestBase : PageTest
    {
        protected static string BaseUrl => ServerFixture.BaseUrl;

        /// <summary>
        /// Signs in as the seeded test admin and waits for the redirect home. After
        /// this the browser context holds the authenticated cookie, which the
        /// context's <c>Page.APIRequest</c> shares.
        /// </summary>
        /// <returns>A task that completes once sign-in has landed on the home page.</returns>
        protected async Task SignInAsync()
        {
            await Page.GotoAsync(BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", ServerFixture.TestPassword);
            await Page.ClickAsync("input[type=submit]");
            await Page.WaitForURLAsync(BaseUrl + "/");
        }

        /// <summary>
        /// Reads a valid antiforgery token from a page that renders one of the admin
        /// forms. The token is scoped to the current context (anonymous before
        /// <see cref="SignInAsync"/>, authenticated after) and <c>Page.APIRequest</c>
        /// shares the cookie jar, so a POST built with it is antiforgery-valid.
        /// </summary>
        /// <param name="path">The page to read the token from; defaults to home.</param>
        /// <returns>The antiforgery token value.</returns>
        protected async Task<string> AntiforgeryTokenAsync(string path = "/")
        {
            await Page.GotoAsync(BaseUrl + path);
            var token = await Page.GetAttributeAsync("input[name='__RequestVerificationToken']", "value");
            return token!;
        }

        /// <summary>
        /// Builds form-urlencoded POST options carrying the antiforgery token and the
        /// given fields, with redirects disabled so the raw status code is visible.
        /// </summary>
        /// <param name="token">The antiforgery token to include.</param>
        /// <param name="fields">Additional form fields to send.</param>
        /// <returns>Options ready to pass to <c>Page.APIRequest.PostAsync</c>.</returns>
        protected static APIRequestContextOptions FormPost(string token, params (string Key, string Value)[] fields)
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
    }
}
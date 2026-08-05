using Microsoft.Playwright.NUnit;

namespace GlaaTrips.UITests
{
    /// <summary>
    /// Guards against a secret leaking back into the rendered public page. The
    /// Google Maps API key used to be hardcoded in <c>Index.cshtml</c>; it now
    /// comes from configuration and the map script is only emitted when a key is
    /// set. The test server configures no key, so the home page must contain no
    /// Google API key at all.
    /// </summary>
    [TestFixture]
    public class CommittedSecretsTests : PageTest
    {
        private static string BaseUrl => ServerFixture.BaseUrl;

        [Test]
        public async Task Home_page_does_not_leak_a_google_maps_api_key()
        {
            var response = await Page.APIRequest.GetAsync(BaseUrl + "/");
            var body = await response.TextAsync();

            // "AIzaSy" is the fixed prefix of every Google API key; catching the
            // prefix guards against any key, not just the one that leaked before.
            Assert.That(body, Does.Not.Contain("AIzaSy"), "no Google Maps API key may appear in the page source");
            Assert.That(body, Does.Not.Contain("maps.googleapis.com"), "the map script must be omitted when no key is configured");
        }
    }
}
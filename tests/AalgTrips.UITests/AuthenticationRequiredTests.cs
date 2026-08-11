using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Pins the site-wide login requirement: an anonymous visitor can see nothing
    /// — not the home page / map, not an album, and not the media (photos,
    /// thumbnails, the marker file) — and is sent to the login page instead. After
    /// signing in the same content is reachable. This is the behavioural guarantee
    /// behind "you must log in before you can see anything".
    /// </summary>
    [TestFixture]
    public class AuthenticationRequiredTests : UITestBase
    {
        [Test]
        public async Task Anonymous_home_page_redirects_to_login()
        {
            await Page.GotoAsync(BaseUrl + "/");
            Assert.That(Page.Url, Does.Contain("/login"));
        }

        [Test]
        public async Task Anonymous_album_page_redirects_to_login()
        {
            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            Assert.That(Page.Url, Does.Contain("/login"));
        }

        [Test]
        public async Task Anonymous_media_request_is_denied()
        {
            // Do not follow the redirect, so the challenge itself is visible rather
            // than the login page it points at.
            var response = await Page.APIRequest.GetAsync(
                BaseUrl + "/albums/markers.json",
                new APIRequestContextOptions { MaxRedirects = 0 });

            Assert.Multiple(() =>
            {
                Assert.That(response.Status, Is.EqualTo(302), "an anonymous media request must be challenged, not served");
                Assert.That(response.Headers["location"], Does.Contain("/login"));
            });
        }

        [Test]
        public async Task Signed_in_visitor_can_load_the_home_page_and_the_markers()
        {
            await SignInAsync();

            var home = await Page.APIRequest.GetAsync(BaseUrl + "/");
            var markers = await Page.APIRequest.GetAsync(BaseUrl + "/albums/markers.json");

            Assert.Multiple(() =>
            {
                Assert.That(home.Ok, Is.True);
                Assert.That(markers.Ok, Is.True);
            });

            var markersBody = await markers.TextAsync();
            Assert.That(markersBody, Does.Contain(ServerFixture.SampleAlbumSlug));
        }
    }
}
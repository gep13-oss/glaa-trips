using Microsoft.Playwright.NUnit;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Baseline coverage for the admin sign-in flow. Note: this proves the login
    /// (cookie) flow only; the known authorization gap (page-handler [Authorize]
    /// is ignored — MVC1001) is covered by dedicated tests in the security pass,
    /// where the fix lands.
    /// </summary>
    [TestFixture]
    public class LoginTests : PageTest
    {
        [Test]
        public async Task Valid_credentials_sign_in_and_reveal_the_admin_form()
        {
            await Page.GotoAsync(ServerFixture.BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", ServerFixture.TestPassword);
            await Page.ClickAsync("input[type=submit]");

            // Successful sign-in redirects to home, which now renders the admin
            // create-album form (id="admin", shown only to authenticated users).
            await Page.WaitForURLAsync(ServerFixture.BaseUrl + "/");
            await Expect(Page.Locator("#admin")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Invalid_credentials_do_not_sign_in()
        {
            await Page.GotoAsync(ServerFixture.BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", "definitely-the-wrong-password");
            await Page.ClickAsync("input[type=submit]");

            // Still anonymous: the admin form must not be present on the home page.
            await Page.GotoAsync(ServerFixture.BaseUrl + "/");
            await Expect(Page.Locator("#admin")).ToHaveCountAsync(0);
        }

        [Test]
        public async Task Wrong_then_correct_password_still_lands_on_home()
        {
            // Regression: a failed attempt is posted from /login, so the browser's
            // Referer becomes /login and gets echoed into the hidden referrer field.
            // The redirect used to follow that referrer and loop back to /login on
            // the next (successful) sign-in, trapping the user on the login page.
            await Page.GotoAsync(ServerFixture.BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", "definitely-the-wrong-password");
            await Page.ClickAsync("input[type=submit]");

            // Back on the re-rendered login page; sign in correctly this time.
            await Expect(Page.Locator("#password")).ToBeVisibleAsync();
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", ServerFixture.TestPassword);
            await Page.ClickAsync("input[type=submit]");

            await Page.WaitForURLAsync(ServerFixture.BaseUrl + "/");
            await Expect(Page.Locator("#admin")).ToBeVisibleAsync();
        }
    }
}
namespace AalgTrips.UITests
{
    /// <summary>
    /// Covers the multi-user, role-based access model: more than one account can
    /// sign in, an admin sees and can use the content-management controls, and a
    /// viewer can browse the site but has no admin controls and cannot mutate
    /// content. Wrong credentials are rejected. Accounts are seeded by
    /// <see cref="ServerFixture"/> (an admin via the legacy "user" config and a
    /// viewer via the "Users" section).
    /// </summary>
    [TestFixture]
    public class UserAccountsTests : UITestBase
    {
        [Test]
        public async Task An_admin_sees_the_management_controls()
        {
            await SignInAsync();
            await Page.GotoAsync(BaseUrl + "/");

            await Expect(Page.Locator("#admin")).ToHaveCountAsync(1);
        }

        [Test]
        public async Task A_viewer_can_browse_but_has_no_admin_controls()
        {
            await SignInAsViewerAsync();

            await Page.GotoAsync(BaseUrl + "/");

            // Viewers see the content...
            await Expect(Page.Locator($"a[href='/album/{ServerFixture.SampleAlbumSlug}/']")).ToHaveCountAsync(1);

            // ...but not the admin create form.
            await Expect(Page.Locator("#admin")).ToHaveCountAsync(0);

            await Page.GotoAsync($"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/");
            await Expect(Page.Locator("#admin")).ToHaveCountAsync(0);
        }

        [Test]
        public async Task A_viewer_cannot_create_an_album()
        {
            await SignInAsViewerAsync();

            // A viewer can obtain a valid antiforgery token (it is not role-bound),
            // so this proves the server enforces the admin role, not just that the
            // UI hides the form.
            var token = await AntiforgeryTokenAsync("/login");

            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/album/new/create/",
                FormPost(
                    token,
                    ("name", "Sneaky Album"),
                    ("description", string.Empty),
                    ("visited", "2026-01-01"),
                    ("latitude", "0"),
                    ("longitude", "0")));

            Assert.That(response.Status, Is.EqualTo(302), "a viewer's create must be forbidden, not performed");

            // The album must not have been created.
            var check = await Page.APIRequest.GetAsync($"{BaseUrl}/album/sneaky-album/");
            Assert.That(check.Status, Is.EqualTo(404));
        }

        [Test]
        public async Task Wrong_credentials_are_rejected()
        {
            await Page.GotoAsync(BaseUrl + "/login");
            await Page.FillAsync("#username", ServerFixture.TestUsername);
            await Page.FillAsync("#password", "definitely-not-the-password");
            await Page.ClickAsync("input[type=submit]");

            // Still signed out: the home page bounces back to the login page.
            await Page.GotoAsync(BaseUrl + "/");
            Assert.That(Page.Url, Does.Contain("/login"));
        }
    }
}
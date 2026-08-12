using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Coverage for the "who was on the trip" people list: the checkboxes in the
    /// Add/Edit modal round-trip through the album's metadata and the chosen people
    /// are shown on the album page. Each test creates a throwaway album and deletes
    /// it again so the shared sample album is left untouched.
    /// </summary>
    [TestFixture]
    public class PeopleMetadataTests : UITestBase
    {
        [Test]
        public async Task People_round_trip_through_the_modal_and_show_on_the_album()
        {
            await SignInAsync();

            var slug = await CreateTripWithPeopleAsync("Amelia", "Gary", "Bailey");

            try
            {
                // The chosen people appear on the album page.
                await Expect(Page.Locator(".album-head__people")).ToContainTextAsync("Amelia");
                await Expect(Page.Locator(".album-head__people")).ToContainTextAsync("Gary");
                await Expect(Page.Locator(".album-head__people")).ToContainTextAsync("Bailey");

                // Re-opening the edit modal shows exactly those ticked.
                await OpenAlbumActionAsync("editDialog");
                await Expect(PersonBox("Amelia")).ToBeCheckedAsync();
                await Expect(PersonBox("Gary")).ToBeCheckedAsync();
                await Expect(PersonBox("Bailey")).ToBeCheckedAsync();
                await Expect(PersonBox("Lynn")).Not.ToBeCheckedAsync();
            }
            finally
            {
                await DeleteAlbumAsync(slug);
            }
        }

        [Test]
        public async Task Editing_the_people_updates_the_album()
        {
            await SignInAsync();

            var slug = await CreateTripWithPeopleAsync("Amelia");

            try
            {
                await Page.GotoAsync($"{BaseUrl}/album/{slug}/");
                await OpenAlbumActionAsync("editDialog");

                // Swap Amelia out for Lynn + Callie.
                await Page.UncheckAsync("input[name='people'][value='Amelia']");
                await Page.CheckAsync("input[name='people'][value='Lynn']");
                await Page.CheckAsync("input[name='people'][value='Callie']");
                await Page.ClickAsync("#btnEdit");
                await Page.WaitForURLAsync(new Regex($"/album/{slug}/$"));

                var people = await Page.Locator(".album-head__people").InnerTextAsync();
                Assert.Multiple(() =>
                {
                    Assert.That(people, Does.Contain("Lynn"), "the newly-added person should show");
                    Assert.That(people, Does.Contain("Callie"), "the newly-added dog should show");
                    Assert.That(people, Does.Not.Contain("Amelia"), "the removed person should be gone");
                });
            }
            finally
            {
                await DeleteAlbumAsync(slug);
            }
        }

        private ILocator PersonBox(string person)
        {
            // The checkbox for a given person in the currently-open edit modal.
            return Page.Locator($"input[name='people'][value='{person}']");
        }

        /// <summary>
        /// Creates a throwaway trip via the home-page "Add trip" modal with the given
        /// people ticked, and returns its slug (taken from the redirect URL).
        /// </summary>
        /// <param name="people">The people checkboxes to tick.</param>
        /// <returns>The created album's slug.</returns>
        private async Task<string> CreateTripWithPeopleAsync(params string[] people)
        {
            await Page.GotoAsync(BaseUrl + "/");
            await OpenAddTripModalAsync();

            await Page.FillAsync("#name", "People " + System.Guid.NewGuid().ToString("N"));
            await Page.FillAsync("#visited", "2026-05-05");
            await Page.FillAsync("#latitude", "51.5074");
            await Page.FillAsync("#longitude", "-0.1278");

            foreach (var person in people)
            {
                await Page.CheckAsync($"input[name='people'][value='{person}']");
            }

            await Page.ClickAsync("#newalbum");
            await Page.WaitForURLAsync(new Regex("/album/[^/]+/$"));

            var match = Regex.Match(Page.Url, "/album/([^/]+)/$");
            return match.Groups[1].Value;
        }

        private async Task DeleteAlbumAsync(string slug)
        {
            var token = await AntiforgeryTokenAsync();
            await Page.APIRequest.PostAsync($"{BaseUrl}/album/{slug}/delete", FormPost(token));
        }
    }
}
using System.Text.RegularExpressions;

namespace AalgTrips.UITests
{
    /// <summary>
    /// Coverage for the extra detail surfaced on a home-page trip tile: a castle
    /// trip shows the castle badge on its cover and the people are listed in the
    /// tile body, so both are visible while scrolling without using the filters. A
    /// plain trip (the seeded sample) shows neither.
    /// </summary>
    [TestFixture]
    public class AlbumTileTests : UITestBase
    {
        [Test]
        public async Task Castle_and_people_are_shown_on_the_trip_tile()
        {
            await SignInAsync();

            var slug = await CreateTripAsync(castle: true, people: new[] { "Amelia", "Gary" });

            try
            {
                await Page.GotoAsync(BaseUrl + "/");

                var tile = Page.Locator($".trip-card[href='/album/{slug}/']");
                await Expect(tile.Locator(".trip-card__castle")).ToBeVisibleAsync();
                await Expect(tile.Locator(".trip-card__people")).ToContainTextAsync("Amelia");
                await Expect(tile.Locator(".trip-card__people")).ToContainTextAsync("Gary");

                // The tile date shows the full day, not just month and year, so the
                // trip created for 7 July 2026 reads "7 JUL 2026".
                await Expect(tile.Locator(".date")).ToHaveTextAsync("7 JUL 2026");

                // The seeded sample trip is not a castle and has no people, so its
                // tile carries neither the badge nor a people line.
                var sample = Page.Locator($".trip-card[href='/album/{ServerFixture.SampleAlbumSlug}/']");
                await Expect(sample.Locator(".trip-card__castle")).ToHaveCountAsync(0);
                await Expect(sample.Locator(".trip-card__people")).ToHaveCountAsync(0);
            }
            finally
            {
                await DeleteAlbumAsync(slug);
            }
        }

        private async Task<string> CreateTripAsync(bool castle, string[] people)
        {
            await Page.GotoAsync(BaseUrl + "/");
            await OpenAddTripModalAsync();

            await Page.FillAsync("#name", "Tile " + System.Guid.NewGuid().ToString("N"));
            await Page.FillAsync("#visited", "2026-07-07");
            await Page.FillAsync("#latitude", "30.0");
            await Page.FillAsync("#longitude", "30.0");

            if (castle)
            {
                await Page.CheckAsync("#castleVisited");
            }

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
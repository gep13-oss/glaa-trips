namespace GlaaTrips.UITests
{
    /// <summary>
    /// The photo page and its admin mutation handlers used to dereference the album
    /// and photo without a null check, so an unknown album or photo name produced a
    /// 500 — and for the public <c>OnGet</c>, to anonymous visitors. These tests pin
    /// the guards: a missing album or photo now yields 404 on the public page and on
    /// the authenticated rename and delete handlers.
    /// </summary>
    [TestFixture]
    public class PhotoNotFoundTests : UITestBase
    {
        [Test]
        public async Task Photo_page_for_an_unknown_album_returns_not_found()
        {
            var response = await Page.APIRequest.GetAsync($"{BaseUrl}/photo/does-not-exist/whatever/");
            Assert.That(response.Status, Is.EqualTo(404));
        }

        [Test]
        public async Task Photo_page_for_an_unknown_photo_returns_not_found()
        {
            var response = await Page.APIRequest.GetAsync($"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/no-such-photo/");
            Assert.That(response.Status, Is.EqualTo(404));
        }

        [Test]
        public async Task Authenticated_rename_of_a_missing_photo_returns_not_found()
        {
            await SignInAsync();
            var token = await AntiforgeryTokenAsync();

            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/no-such-photo/rename",
                FormPost(token, ("name", "whatever")));

            Assert.That(response.Status, Is.EqualTo(404));
        }

        [Test]
        public async Task Authenticated_delete_of_a_missing_photo_returns_not_found()
        {
            await SignInAsync();
            var token = await AntiforgeryTokenAsync();

            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/no-such-photo/delete",
                FormPost(token));

            Assert.That(response.Status, Is.EqualTo(404));
        }
    }
}
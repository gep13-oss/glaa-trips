namespace GlaaTrips.UITests
{
    /// <summary>
    /// The photo page and its admin mutation handlers used to dereference the album
    /// and photo without a null check, so an unknown album or photo name produced a
    /// 500. These tests pin the guards: for a signed-in visitor a missing album or
    /// photo now yields 404 on the page and on the rename and delete handlers. (The
    /// whole site requires authentication, so every case signs in first.)
    /// </summary>
    [TestFixture]
    public class PhotoNotFoundTests : UITestBase
    {
        [SetUp]
        public async Task SignIn()
        {
            await SignInAsync();
        }

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
            var token = await AntiforgeryTokenAsync();

            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/no-such-photo/rename",
                FormPost(token, ("name", "whatever")));

            Assert.That(response.Status, Is.EqualTo(404));
        }

        [Test]
        public async Task Authenticated_delete_of_a_missing_photo_returns_not_found()
        {
            var token = await AntiforgeryTokenAsync();

            var response = await Page.APIRequest.PostAsync(
                $"{BaseUrl}/photo/{ServerFixture.SampleAlbumSlug}/no-such-photo/delete",
                FormPost(token));

            Assert.That(response.Status, Is.EqualTo(404));
        }
    }
}
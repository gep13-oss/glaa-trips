namespace AalgTrips.UITests
{
    /// <summary>
    /// Behavioural coverage for the admin forms now living behind buttons: the
    /// home-page "Add trip" modal and the album-page "Actions" dropdown that opens
    /// the Edit / Rename / Upload modals. These lock in the interaction contract —
    /// the forms are hidden until asked for, a button reveals them, and the modal
    /// closes on Cancel or Escape — while the create/edit/rename side-effects stay
    /// covered by <see cref="AlbumEditTests"/> and <see cref="AdminAuthorizationTests"/>.
    /// </summary>
    [TestFixture]
    public class ModalFormsTests : UITestBase
    {
        private static string AlbumUrl => $"{BaseUrl}/album/{ServerFixture.SampleAlbumSlug}/";

        [Test]
        public async Task Add_trip_form_is_hidden_until_the_button_opens_the_modal()
        {
            await SignInAsync();

            // The create form must not be permanently rendered — it lives in a
            // closed <dialog> until the admin asks for it.
            await Expect(Page.Locator("#addTripDialog")).ToBeHiddenAsync();
            await Expect(Page.Locator("#name")).ToBeHiddenAsync();

            await OpenAddTripModalAsync();

            await Expect(Page.Locator("#addTripDialog")).ToBeVisibleAsync();
            await Expect(Page.Locator("#name")).ToBeVisibleAsync();
            await Expect(Page.Locator("#newalbum")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Add_trip_modal_closes_on_cancel()
        {
            await SignInAsync();
            await OpenAddTripModalAsync();

            await Page.ClickAsync("#addTripDialog .field--actions .btn--ghost");

            await Expect(Page.Locator("#addTripDialog")).ToBeHiddenAsync();
            await Expect(Page.Locator("#name")).ToBeHiddenAsync();
        }

        [Test]
        public async Task Add_trip_modal_closes_on_escape()
        {
            await SignInAsync();
            await OpenAddTripModalAsync();

            await Page.Keyboard.PressAsync("Escape");

            await Expect(Page.Locator("#addTripDialog")).ToBeHiddenAsync();
        }

        [Test]
        public async Task Album_actions_are_hidden_until_the_menu_is_opened()
        {
            await SignInAsync();
            await Page.GotoAsync(AlbumUrl);

            // The Actions trigger is always shown to an admin, but its items and the
            // dialogs they open must stay hidden until the menu is opened.
            await Expect(Page.Locator("summary.actions-menu__trigger")).ToBeVisibleAsync();
            await Expect(Page.Locator("[data-open-dialog='#editDialog']")).ToBeHiddenAsync();
            await Expect(Page.Locator("#deletealbum")).ToBeHiddenAsync();
            await Expect(Page.Locator("#editDialog")).ToBeHiddenAsync();

            await OpenActionsMenuAsync();

            await Expect(Page.Locator("[data-open-dialog='#editDialog']")).ToBeVisibleAsync();
            await Expect(Page.Locator("[data-open-dialog='#renameDialog']")).ToBeVisibleAsync();
            await Expect(Page.Locator("[data-open-dialog='#uploadDialog']")).ToBeVisibleAsync();
            await Expect(Page.Locator("#deletealbum")).ToBeVisibleAsync();
        }

        [Test]
        public async Task Edit_trip_modal_opens_from_the_actions_menu()
        {
            await SignInAsync();
            await Page.GotoAsync(AlbumUrl);

            await OpenAlbumActionAsync("editDialog");

            await Expect(Page.Locator("#editDialog")).ToBeVisibleAsync();
            await Expect(Page.Locator("#btnEdit")).ToBeVisibleAsync();

            // The edit form is pre-filled with the album's current details.
            await Expect(Page.Locator("#editDialog #name")).ToHaveValueAsync(ServerFixture.SampleAlbumTitle);
        }
    }
}
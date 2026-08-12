using System.Collections.Generic;

namespace AalgTrips.Models
{
    /// <summary>
    /// The canonical set of people offered as checkboxes on the Add/Edit trip form.
    /// A trip's stored people list is free text, so an album may also carry names
    /// that are not in this list; those still display and stay selectable. Edit this
    /// list to change which names appear as ready-made options.
    /// </summary>
    public static class KnownPeople
    {
        /// <summary>Gets the people shown, in display order, as trip-companion checkboxes.</summary>
        public static IReadOnlyList<string> All { get; } = new[]
        {
            "Amelia",
            "Alivia",
            "Lynn",
            "Gary",
            "Granny Park",
            "Granda Park",
            "Granny Milne",
            "Granda Milne",
            "Bailey",
            "Callie",
        };
    }
}
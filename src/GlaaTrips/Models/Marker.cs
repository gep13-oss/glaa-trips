namespace GlaaTrips.Models
{
    /// <summary>
    /// A single point on the home-page map. Serialized to <c>markers.json</c> and
    /// read by <c>wwwroot/js/map.js</c>, which draws a Leaflet marker at
    /// <see cref="Lat"/>/<see cref="Long"/>, shows <see cref="Name"/>,
    /// <see cref="Date"/> and <see cref="Photos"/> in a hover tooltip, and links
    /// through to the album identified by <see cref="Slug"/> on click. The property
    /// names are PascalCase because the client reads them verbatim from the
    /// default-serialized JSON (e.g. <c>marker.Name</c>).
    /// </summary>
    public class Marker
    {
        public double Lat { get; set; }

        public double Long { get; set; }

        public string Slug { get; set; }

        /// <summary>Gets or sets the album's display name, shown as the tooltip heading.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the trip date pre-formatted for display (e.g. <c>Jan 2026</c>).
        /// Formatted server-side in the invariant culture so the client needs no date
        /// parsing and the label is stable regardless of the viewer's locale.
        /// </summary>
        public string Date { get; set; }

        /// <summary>Gets or sets the number of photos in the album, shown in the tooltip.</summary>
        public int Photos { get; set; }
    }
}
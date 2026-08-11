using System;

namespace AalgTrips.Models
{
    public class AlbumMetaData
    {
        public string DisplayName { get; set; }

        public string Description { get; set; }

        public DateTime Visited { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the file name of the photo chosen to represent the album
        /// (its cover on the home page). When empty, the album falls back to its
        /// first photo.
        /// </summary>
        public string CoverPhoto { get; set; }
    }
}
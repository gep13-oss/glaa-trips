using System;
using System.Collections.Generic;

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
        /// Gets or sets a value indicating whether this trip visited a castle. Drives
        /// a distinct-coloured pin on the home-page map. Absent from older metadata
        /// files, where it deserializes to <c>false</c>.
        /// </summary>
        public bool CastleVisited { get; set; }

        /// <summary>
        /// Gets or sets the people who were on the trip. Free-text names (not tied to
        /// site accounts), chosen from a canonical checkbox set but able to hold any
        /// name. Absent from older metadata files, where it deserializes to
        /// <c>null</c> and is treated as an empty list.
        /// </summary>
        public List<string> People { get; set; }

        /// <summary>
        /// Gets or sets the file name of the photo chosen to represent the album
        /// (its cover on the home page). When empty, the album falls back to its
        /// first photo.
        /// </summary>
        public string CoverPhoto { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GlaaTrips.Models
{
    public class Album : IPaginator
    {
        private readonly AlbumCollection _ac;
        private readonly object _sync = new object();

        public Album(string absolutePath, AlbumCollection ac)
            : this(absolutePath, ac, null)
        {
        }

        public Album(string absolutePath, AlbumCollection ac, AlbumMetaData metaData)
        {
            _ac = ac;
            AbsolutePath = absolutePath;
            Id = new DirectoryInfo(AbsolutePath).Name;
            Photos = new List<Photo>();

            if (metaData != null)
            {
                DisplayName = metaData.DisplayName;
                Description = metaData.Description;
                Visited = metaData.Visited;
                Latitude = metaData.Latitude;
                Longitude = metaData.Longitude;
            }
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public DateTime Visited { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        /// <summary>
        /// Gets the album's location formatted as a map reference for display, e.g.
        /// <c>57.42°N 1.86°W</c>. Latitude and longitude are shown as absolute
        /// values with a hemisphere letter, always in the invariant culture so the
        /// decimal point is stable regardless of the server's locale.
        /// </summary>
        public string Coordinates
        {
            get
            {
                string ns = Latitude >= 0 ? "N" : "S";
                string ew = Longitude >= 0 ? "E" : "W";
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.00}°{1} {2:0.00}°{3}",
                    Math.Abs(Latitude),
                    ns,
                    Math.Abs(Longitude),
                    ew);
            }
        }

        public string UrlName
        {
            get
            {
                return Id.Replace(" ", "%20").ToLowerInvariant();
            }
        }

        public string Link
        {
            get
            {
                return $"/album/{UrlName}/";
            }
        }

        public string AbsolutePath { get; }

        public List<Photo> Photos { get; private set; }

        public Photo CoverPhoto
        {
            get
            {
                return Photos?.FirstOrDefault();
            }
        }

        public IPaginator Next
        {
            get
            {
                int index = _ac.Albums.IndexOf(this);

                if (index < _ac.Albums.Count - 1)
                {
                    return _ac.Albums[index + 1];
                }

                return null;
            }
        }

        public IPaginator Previous
        {
            get
            {
                int index = _ac.Albums.IndexOf(this);

                if (index > 0)
                {
                    return _ac.Albums[index - 1];
                }

                return null;
            }
        }

        /// <summary>
        /// Adds photos to the album and re-sorts. Like <see cref="AlbumCollection"/>,
        /// the album's <see cref="Photos"/> list is mutated copy-on-write under a
        /// lock so concurrent readers always enumerate a stable snapshot.
        /// </summary>
        /// <param name="photos">The photos to add.</param>
        public void AddPhotos(IEnumerable<Photo> photos)
        {
            lock (_sync)
            {
                var updated = new List<Photo>(Photos);
                updated.AddRange(photos);
                Photos = updated.OrderBy(p => p.DisplayName).ToList();
            }
        }

        /// <summary>
        /// Removes a photo from the album, if it is present.
        /// </summary>
        /// <param name="photo">The photo to remove.</param>
        public void RemovePhoto(Photo photo)
        {
            lock (_sync)
            {
                Photos = Photos.Where(p => p != photo).ToList();
            }
        }

        /// <summary>
        /// Replaces <paramref name="oldPhoto"/> with <paramref name="newPhoto"/>
        /// (for example after a rename) and re-sorts.
        /// </summary>
        /// <param name="oldPhoto">The photo being replaced.</param>
        /// <param name="newPhoto">The photo to put in its place.</param>
        public void ReplacePhoto(Photo oldPhoto, Photo newPhoto)
        {
            lock (_sync)
            {
                var updated = Photos.Where(p => p != oldPhoto).ToList();
                updated.Add(newPhoto);
                Photos = updated.OrderBy(p => p.DisplayName).ToList();
            }
        }

        /// <summary>
        /// Sorts the photos in the album.
        /// </summary>
        public void Sort()
        {
            lock (_sync)
            {
                Photos = Photos.OrderBy(p => p.DisplayName).ToList();
            }
        }
    }
}
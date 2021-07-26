using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace glaa_trips.Models
{
    public class Album : IPaginator
    {
        private AlbumCollection _ac;

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
        /// Sorts the photos in the album.
        /// </summary>
        public void Sort()
        {
            Photos = Photos.OrderBy(p => p.DisplayName).ToList();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GlaaTrips.Models
{
    public class Photo : IPaginator
    {
        private readonly Dictionary<int, int> _heights = new Dictionary<int, int>();
        private static readonly Regex _size = new Regex(@"-(?<width>[0-9]+)x(?<height>[0-9]+)\.", RegexOptions.Compiled);

        public Photo(Album album, string fileName)
        {
            Album = album;
            Id = fileName;
        }

        public string Id { get; private set; }

        public string DisplayName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Id);
            }
        }

        public string UrlName
        {
            get
            {
                return DisplayName.Replace(" ", "%20").ToLowerInvariant();
            }
        }

        public Album Album { get; }

        public IPaginator Next
        {
            get
            {
                int index = Album.Photos.IndexOf(this);

                if (index < Album.Photos.Count - 1)
                {
                    return Album.Photos[index + 1];
                }

                return null;
            }
        }

        public IPaginator Previous
        {
            get
            {
                int index = Album.Photos.IndexOf(this);

                if (index > 0)
                {
                    return Album.Photos[index - 1];
                }

                return null;
            }
        }

        public string Link
        {
            get
            {
                return $"/photo/{Album.UrlName}/{UrlName}/";
            }
        }

        public string DownloadLink
        {
            get
            {
                return Album.Store.PhotoUrl(Album.Id, Id);
            }
        }

        /// <summary>
        /// Resolves the served URL of the thumbnail generated at the given width
        /// and reports its height. The matching thumbnail is looked up from the
        /// store (by the <c>{name}-{width}x{height}{ext}</c> convention) and the
        /// resolved height is cached per width so repeated calls do not re-query.
        /// </summary>
        /// <param name="width">The thumbnail width to resolve.</param>
        /// <param name="height">The resolved thumbnail height, or <c>0</c> when there is no such thumbnail.</param>
        /// <returns>The thumbnail URL, or <c>null</c> when no thumbnail of that width exists.</returns>
        public string GetThumbnailLink(int width, out int height)
        {
            if (_heights.TryGetValue(width, out height))
            {
                return Album.Store.ThumbnailUrl(Album.Id, ThumbnailFileName(width, height));
            }

            foreach (var thumbnail in Album.Store.ListThumbnailFileNames(Album.Id))
            {
                if (!PhotoStoreConventions.ThumbnailBelongsTo(thumbnail, Id))
                {
                    continue;
                }

                Match match = _size.Match(thumbnail);

                if (match.Success && int.Parse(match.Groups["width"].Value) == width)
                {
                    height = int.Parse(match.Groups["height"].Value);
                    _heights[width] = height;
                    return Album.Store.ThumbnailUrl(Album.Id, thumbnail);
                }
            }

            height = 0;
            return null;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        private string ThumbnailFileName(int width, int height)
        {
            string ext = Path.GetExtension(Id);
            return $"{DisplayName}-{width}x{height}{ext}";
        }
    }
}
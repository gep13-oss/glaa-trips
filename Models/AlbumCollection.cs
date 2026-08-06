using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace GlaaTrips.Models
{
    /// <summary>
    /// The in-memory catalogue of albums. It is registered as a singleton and is
    /// therefore shared across every request: public GET requests enumerate it
    /// while admin handlers add, remove and reload entries. To keep that safe
    /// without forcing every reader to lock, all mutation goes through the methods
    /// below under <see cref="_sync"/>, and each one builds a brand-new list and
    /// swaps the <see cref="Albums"/> reference (copy-on-write). A reader only ever
    /// sees a fully-published list that is never mutated in place, so it cannot
    /// throw a "collection was modified" error or observe a half-applied change.
    /// </summary>
    public class AlbumCollection
    {
        private readonly IWebHostEnvironment _environment;
        private readonly object _sync = new object();
        private static readonly string[] _extensions = { ".jpg", ".jpeg", ".gif", ".png" };

        public AlbumCollection(IWebHostEnvironment environment)
        {
            _environment = environment;
            Albums = new List<Album>();

            Initialize(environment.WebRootPath);
        }

        public List<Album> Albums { get; private set; }

        public bool IsImageFile(string file)
        {
            string ext = Path.GetExtension(file);
            return _extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a newly created album and re-sorts the collection.
        /// </summary>
        /// <param name="album">The album to add.</param>
        public void Add(Album album)
        {
            lock (_sync)
            {
                var updated = new List<Album>(Albums) { album };
                Albums = updated.OrderBy(a => a.Id).ToList();
            }
        }

        /// <summary>
        /// Removes the album whose <see cref="Album.Id"/> matches
        /// <paramref name="id"/>, if it is present.
        /// </summary>
        /// <param name="id">The id (folder name) of the album to remove.</param>
        public void Remove(string id)
        {
            lock (_sync)
            {
                Albums = Albums
                    .Where(a => !a.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// Reloads a single album from disk (its metadata and its photos) and swaps
        /// the fresh instance into the collection, replacing any existing album with
        /// the same id. This is how an edit that rewrote the album's
        /// <c>data.json</c> is reflected without losing the album's photos or its
        /// absolute path.
        /// </summary>
        /// <param name="absoluteAlbumPath">The absolute path of the album folder to reload.</param>
        public void ReloadFromDisk(string absoluteAlbumPath)
        {
            var reloaded = GetAlbum(absoluteAlbumPath);

            lock (_sync)
            {
                var updated = Albums
                    .Where(a => !a.Id.Equals(reloaded.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                updated.Add(reloaded);
                Albums = updated.OrderBy(a => a.Id).ToList();
            }
        }

        /// <summary>
        /// Rewrites <c>markers.json</c> from the current album set so the map stays
        /// in step after a create, edit or delete. The marker list is snapshotted
        /// under the lock; the file write happens outside it.
        /// </summary>
        /// <returns>A task that completes when the file has been written.</returns>
        public async Task WriteMarkersAsync()
        {
            List<Marker> markers;

            lock (_sync)
            {
                markers = Albums
                    .Select(a => new Marker { Lat = a.Latitude, Long = a.Longitude, Slug = a.Id })
                    .ToList();
            }

            string markerJsonPath = Path.Combine(_environment.WebRootPath, "albums", "markers.json");

            using var createStream = File.Create(markerJsonPath);
            await JsonSerializer.SerializeAsync(createStream, markers);
        }

        private void Initialize(string contentPath)
        {
            var root = Path.Combine(contentPath, "albums");
            if (!Directory.Exists(root))
            {
                return;
            }

            var albums = new List<Album>();

            foreach (string albumPath in Directory.EnumerateDirectories(root))
            {
                albums.Add(GetAlbum(albumPath));
            }

            Albums = albums.OrderBy(a => a.Id).ToList();
        }

        private Album GetAlbum(string albumPath)
        {
            var metadataFileName = Path.Combine(albumPath, "data.json");
            Album album;

            if (File.Exists(metadataFileName))
            {
                var albumMetaData = JsonSerializer.Deserialize<AlbumMetaData>(File.ReadAllText(metadataFileName));
                album = new Album(albumPath, this, albumMetaData);
            }
            else
            {
                album = new Album(albumPath, this);
            }

            var directory = new DirectoryInfo(albumPath);
            var photos = directory.EnumerateFiles()
                .Where(f => IsImageFile(f.FullName))
                .Select(a => new Photo(album, a));

            album.AddPhotos(photos);

            return album;
        }
    }
}
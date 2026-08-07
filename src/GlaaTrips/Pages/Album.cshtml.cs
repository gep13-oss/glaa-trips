using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GlaaTrips.Pages
{
    public class AlbumsModel : AdminHandlerPageModel
    {
        private readonly AlbumCollection _ac;
        private readonly IPhotoStore _store;
        private readonly ImageProcessor _processor;

        public AlbumsModel(AlbumCollection ac, IPhotoStore store, ImageProcessor processor)
        {
            _ac = ac;
            _store = store;
            _processor = processor;
        }

        public Album Album { get; private set; }

        public IActionResult OnGet(string name)
        {
            Album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (Album == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDelete(string name)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            if (!SafePathHelper.IsValidSegment(name))
            {
                return BadRequest();
            }

            await _store.DeleteAlbumAsync(name);

            _ac.Remove(name);
            await _ac.WriteMarkersAsync();

            return new RedirectResult("~/");
        }

        public async Task<IActionResult> OnPostCreate(string name, string description, string visited, double latitude, double longitude)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            string slugName = SlugHelper.GenerateSlug(name);

            // The slug is normally already separator-free, but an all-punctuation
            // title can slug to an empty string. Reject anything that is not a safe
            // single segment before it is used as an album id.
            if (!SafePathHelper.IsValidSegment(slugName))
            {
                return BadRequest();
            }

            var albumMetaData = new AlbumMetaData
            {
                DisplayName = name,
                Description = description,
                Visited = DateTime.Parse(visited),
                Latitude = latitude,
                Longitude = longitude,
            };

            await _store.WriteMetadataAsync(slugName, albumMetaData);

            _ac.Add(new Album(slugName, _ac, albumMetaData));
            await _ac.WriteMarkersAsync();

            return new RedirectResult($"~/album/{slugName}/");
        }

        public async Task<IActionResult> OnPostEdit([FromRoute(Name = "name")] string slug, string name, string description, string visited, double latitude, double longitude)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            // The album slug is the route value. It is bound explicitly from the
            // route because the edit form also posts a "name" field (the album's
            // display name), and Razor Pages' default binder would otherwise let
            // that form value win.
            if (!SafePathHelper.IsValidSegment(slug))
            {
                return BadRequest();
            }

            var existingAlbum = _ac.Albums.FirstOrDefault(a => a.Id.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (existingAlbum == null)
            {
                return NotFound();
            }

            var albumMetaData = new AlbumMetaData
            {
                DisplayName = name,
                Description = description,
                Visited = DateTime.Parse(visited),
                Latitude = latitude,
                Longitude = longitude,

                // Editing the trip details must not drop the chosen cover photo.
                CoverPhoto = existingAlbum.CoverPhotoName,
            };

            await _store.WriteMetadataAsync(slug, albumMetaData);

            // Reload from the store so the refreshed album keeps its photos, then
            // rewrite the markers so a moved pin is reflected on the map.
            _ac.ReloadAlbum(slug);
            await _ac.WriteMarkersAsync();

            return new RedirectResult($"~/album/{slug}/");
        }

        public async Task<IActionResult> OnPostUpload(string name, ICollection<IFormFile> files)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (album == null)
            {
                return NotFound();
            }

            var uploaded = new List<Photo>();

            foreach (var file in files.Where(f => _ac.IsImageFile(f.FileName)))
            {
                string fileName = Path.GetFileName(file.FileName);

                if (_store.PhotoExists(album.Id, fileName))
                {
                    // Keep both when a name collides, mirroring the previous
                    // behaviour of tagging the duplicate with the upload's hash.
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)}.{file.GetHashCode()}{Path.GetExtension(fileName)}";
                }

                // Persist the original first, then derive thumbnails from the saved
                // file, so a decode failure never leaves a half-written original
                // masquerading as a real photo.
                using (var uploadStream = file.OpenReadStream())
                {
                    await _store.SavePhotoAsync(album.Id, fileName, uploadStream);
                }

                IReadOnlyList<GeneratedThumbnail> thumbnails;

                using (var savedImage = _store.OpenPhoto(album.Id, fileName))
                {
                    thumbnails = _processor.CreateThumbnails(savedImage, fileName);
                }

                if (thumbnails.Count == 0)
                {
                    // The bytes were not a decodable image despite the extension;
                    // drop the saved original and skip it rather than 500.
                    await _store.DeletePhotoAsync(album.Id, fileName);
                    continue;
                }

                foreach (var thumbnail in thumbnails)
                {
                    using var thumbnailStream = new MemoryStream(thumbnail.Content);
                    await _store.SaveThumbnailAsync(album.Id, thumbnail.FileName, thumbnailStream);
                }

                uploaded.Add(new Photo(album, fileName));
            }

            album.AddPhotos(uploaded);

            return new RedirectResult($"~/album/{WebUtility.UrlEncode(name).Replace('+', ' ')}/");
        }

        public async Task<IActionResult> OnPostCover([FromRoute(Name = "name")] string slug, string photo)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            if (!SafePathHelper.IsValidSegment(slug) || !SafePathHelper.IsValidSegment(photo))
            {
                return BadRequest();
            }

            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (album == null)
            {
                return NotFound();
            }

            // The chosen cover must be a photo that is actually in the album.
            if (!album.Photos.Any(p => p.Id.Equals(photo, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            var albumMetaData = new AlbumMetaData
            {
                DisplayName = album.DisplayName,
                Description = album.Description,
                Visited = album.Visited,
                Latitude = album.Latitude,
                Longitude = album.Longitude,
                CoverPhoto = photo,
            };

            await _store.WriteMetadataAsync(slug, albumMetaData);
            _ac.ReloadAlbum(slug);

            return new RedirectResult($"~/album/{slug}/");
        }
    }
}
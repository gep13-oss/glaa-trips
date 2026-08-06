using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Slugify;

namespace GlaaTrips.Pages
{
    public class AlbumsModel : AdminHandlerPageModel
    {
        private readonly AlbumCollection _ac;
        private readonly IWebHostEnvironment _environment;
        private readonly ImageProcessor _processor;

        public AlbumsModel(AlbumCollection ac, IWebHostEnvironment environment, ImageProcessor processor)
        {
            _ac = ac;
            _environment = environment;
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

            string albumsRoot = Path.Combine(_environment.WebRootPath, "albums");

            if (!SafePathHelper.TryCombineWithin(albumsRoot, name, out string path))
            {
                return BadRequest();
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

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

            SlugHelper helper = new SlugHelper();
            string slugName = helper.GenerateSlug(name);

            // The slug is normally already separator-free, but an all-punctuation
            // title can slug to an empty string, which would resolve to the albums
            // root itself. Reject anything that is not a safe single segment.
            if (!SafePathHelper.IsValidSegment(slugName))
            {
                return BadRequest();
            }

            string path = Path.Combine(_environment.WebRootPath, "albums", slugName);

            Directory.CreateDirectory(path);

            var metadataFileName = Path.Combine(path, "data.json");
            var albumMetaData = new AlbumMetaData();
            albumMetaData.DisplayName = name;
            albumMetaData.Description = description;
            albumMetaData.Visited = DateTime.Parse(visited);
            albumMetaData.Latitude = latitude;
            albumMetaData.Longitude = longitude;

            using (var createStream = System.IO.File.Create(metadataFileName))
            {
                await JsonSerializer.SerializeAsync<AlbumMetaData>(createStream, albumMetaData);
            }

            _ac.Add(new Album(path, _ac, albumMetaData));
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
            // that form value win. The previous implementation scraped the slug
            // out of the request path with a "/Album/" regex that never matched
            // the lower-case route, so every edit fell through to BadRequest.
            string albumsRoot = Path.Combine(_environment.WebRootPath, "albums");

            if (!SafePathHelper.TryCombineWithin(albumsRoot, slug, out string path))
            {
                return BadRequest();
            }

            var existingAlbum = _ac.Albums.FirstOrDefault(a => a.Id.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (existingAlbum == null)
            {
                return NotFound();
            }

            var metadataFileName = Path.Combine(path, "data.json");
            var albumMetaData = new AlbumMetaData();
            albumMetaData.DisplayName = name;
            albumMetaData.Description = description;
            albumMetaData.Visited = DateTime.Parse(visited);
            albumMetaData.Latitude = latitude;
            albumMetaData.Longitude = longitude;

            using (var createStream = System.IO.File.Create(metadataFileName))
            {
                await JsonSerializer.SerializeAsync<AlbumMetaData>(createStream, albumMetaData);
            }

            // Reload from disk so the refreshed album keeps its absolute path and
            // its photos, then rewrite markers.json so a moved pin is reflected on
            // the map. The old code built `new Album(slug, ...)`, which set a
            // relative path and an empty photo list, and never touched markers.json.
            _ac.ReloadFromDisk(path);
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

            var uploaded = new List<Photo>();

            foreach (var file in files.Where(f => _ac.IsImageFile(f.FileName)))
            {
                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(_environment.WebRootPath, "albums", album.Id, Path.GetFileName(fileName));

                if (System.IO.File.Exists(filePath))
                {
                    filePath = Path.ChangeExtension(filePath, file.GetHashCode() + Path.GetExtension(filePath));
                }

                using (var imageStream = file.OpenReadStream())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    _processor.CreateThumbnails(imageStream, filePath);
                    await file.CopyToAsync(fileStream);
                }

                uploaded.Add(new Photo(album, new FileInfo(filePath)));
            }

            album.AddPhotos(uploaded);

            return new RedirectResult($"~/album/{WebUtility.UrlEncode(name).Replace('+', ' ')}/");
        }
    }
}
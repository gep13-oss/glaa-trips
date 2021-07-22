﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using glaa_trips.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Slugify;

namespace glaa_trips.Pages
{
    public class AlbumsModel : PageModel
    {
        private AlbumCollection _ac;
        private IHostingEnvironment _environment;
        private ImageProcessor _processor;

        public AlbumsModel(AlbumCollection ac, IHostingEnvironment environment, ImageProcessor processor)
        {
            _ac = ac;
            _environment = environment;
            _processor = processor;
        }

        public Album Album { get; private set; }

        public IActionResult OnGet(string name)
        {
            Album = _ac.Albums.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (Album == null)
            {
                return NotFound();
            }

            return Page();
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult OnPostDelete(string name)
        {
            string path = Path.Combine(_environment.WebRootPath, "albums", name);

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            var album = _ac.Albums.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (album != null)
            {
                _ac.Albums.Remove(album);
            }

            return new RedirectResult("~/");
        }

        [Authorize]
        public async Task<IActionResult> OnPostCreate(string name, string description, string visited, double latitude, double longitude)
        {
            string markerJsonPath = Path.Combine(_environment.WebRootPath, "albums", "markers.json");
            
            SlugHelper helper = new SlugHelper();
            string slugName = helper.GenerateSlug(name);

            List<Marker> markers = null;

            if (System.IO.File.Exists(markerJsonPath))
            {
                markers = JsonSerializer.Deserialize<List<Marker>>(System.IO.File.ReadAllText(markerJsonPath));
            }
            else
            {
                markers = new List<Marker>();
            }

            string path = Path.Combine(_environment.WebRootPath, "albums", slugName);

            Directory.CreateDirectory(path);

            var metadataFileName = Path.Combine(path, "data.json");
            var albumMetaData = new AlbumMetaData();
            albumMetaData.Description = description;
            albumMetaData.Visited = DateTime.Parse(visited);
            albumMetaData.Latitude = latitude;
            albumMetaData.Longitude = longitude;

            var marker = new Marker();
            marker.Lat = latitude;
            marker.Long = longitude;
            marker.Slug = slugName;

            markers.Add(marker);

            using (var createStream = System.IO.File.Create(metadataFileName))
            {
                await JsonSerializer.SerializeAsync<AlbumMetaData>(createStream, albumMetaData);
            };

            using (var createStream = System.IO.File.Create(markerJsonPath))
            {
                await JsonSerializer.SerializeAsync<List<Marker>>(createStream, markers);
            };

            var album = new Album(path, _ac, albumMetaData);
                  
            _ac.Albums.Insert(0, album);
            _ac.Sort();

            return new RedirectResult($"~/album/{slugName}/");
        }

        [Authorize]
        public async Task<IActionResult> OnPostUpload(string name, ICollection<IFormFile> files)
        {
            var album = _ac.Albums.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            foreach (var file in files.Where(f => _ac.IsImageFile(f.FileName)))
            {
                string fileName = Path.GetFileName(file.FileName);
                string filePath = Path.Combine(_environment.WebRootPath, "albums", album.Name, Path.GetFileName(fileName));

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

                var photo = new Photo(album, new FileInfo(filePath));
                album.Photos.Insert(0, photo);
            }

            album.Sort();

            return new RedirectResult($"~/album/{WebUtility.UrlEncode(name).Replace('+', ' ')}/");
        }
    }
}

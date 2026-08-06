using System;
using System.IO;
using System.Linq;
using System.Net;
using GlaaTrips.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace GlaaTrips.Pages
{
    public class PhotoModel : AdminHandlerPageModel
    {
        private readonly AlbumCollection _ac;
        private readonly IWebHostEnvironment _environment;

        public PhotoModel(AlbumCollection ac, IWebHostEnvironment environment)
        {
            _ac = ac;
            _environment = environment;
        }

        public Photo Photo { get; set; }

        public void OnGet(string albumName, string photoName)
        {
            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(albumName, StringComparison.OrdinalIgnoreCase));
            Photo = album.Photos.FirstOrDefault(p => p.DisplayName.Equals(photoName, StringComparison.OrdinalIgnoreCase));
        }

        public IActionResult OnPostRename(string albumName, string photoName)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            string requestedName = Request.Form["name"];

            if (!SafePathHelper.IsValidSegment(requestedName))
            {
                return BadRequest();
            }

            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(albumName, StringComparison.OrdinalIgnoreCase));
            Photo = album.Photos.FirstOrDefault(p => p.DisplayName.Equals(photoName, StringComparison.OrdinalIgnoreCase));
            string name = requestedName + Path.GetExtension(Photo.AbsolutePath);

            var newPhotoPath = new FileInfo(Path.Combine(album.AbsolutePath, name));

            System.IO.File.Move(Photo.AbsolutePath, newPhotoPath.FullName);
            var newPhoto = new Photo(album, newPhotoPath);

            album.ReplacePhoto(Photo, newPhoto);

            // Rename thumbnails
            string folder = Path.Combine(album.AbsolutePath, "thumbnail");
            var pattern = $"{Photo.DisplayName}-*x*{Path.GetExtension(Photo.AbsolutePath)}";

            foreach (var file in Directory.EnumerateFiles(folder, pattern))
            {
                string newThumbnail = Path.Combine(folder, Path.GetFileName(file).Replace(Photo.DisplayName, newPhoto.DisplayName));
                System.IO.File.Move(file, newThumbnail);
            }

            return new RedirectResult($"~/photo/{WebUtility.UrlEncode(albumName).Replace('+', ' ')}/{newPhoto.DisplayName}/");
        }

        public IActionResult OnPostDelete(string albumName, string photoName)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(albumName, StringComparison.OrdinalIgnoreCase));
            Photo = album.Photos.FirstOrDefault(p => p.DisplayName.Equals(photoName, StringComparison.OrdinalIgnoreCase));
            album.RemovePhoto(Photo);

            if (System.IO.File.Exists(Photo.AbsolutePath))
            {
                System.IO.File.Delete(Photo.AbsolutePath);
                string folder = Path.Combine(album.AbsolutePath, "thumbnail");
                var pattern = $"{Photo.DisplayName}-*x*{Path.GetExtension(Photo.AbsolutePath)}";

                foreach (var file in Directory.EnumerateFiles(folder, pattern))
                {
                    System.IO.File.Delete(file);
                }
            }

            return new RedirectResult($"~/album/{WebUtility.UrlEncode(albumName).Replace('+', ' ')}/");
        }
    }
}
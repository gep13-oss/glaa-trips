using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AalgTrips.Models;
using Microsoft.AspNetCore.Mvc;

namespace AalgTrips.Pages
{
    public class PhotoModel : AdminHandlerPageModel
    {
        private readonly AlbumCollection _ac;
        private readonly IPhotoStore _store;

        public PhotoModel(AlbumCollection ac, IPhotoStore store)
        {
            _ac = ac;
            _store = store;
        }

        public Photo Photo { get; set; }

        public IActionResult OnGet(string albumName, string photoName)
        {
            Photo = FindPhoto(albumName, photoName);

            if (Photo == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRename(string albumName, string photoName)
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

            Photo = FindPhoto(albumName, photoName);

            if (Photo == null)
            {
                return NotFound();
            }

            var album = Photo.Album;
            string newFileName = requestedName + Path.GetExtension(Photo.Id);

            await _store.RenamePhotoAsync(album.Id, Photo.Id, newFileName);

            var newPhoto = new Photo(album, newFileName);
            album.ReplacePhoto(Photo, newPhoto);

            return new RedirectResult($"~/photo/{WebUtility.UrlEncode(albumName).Replace('+', ' ')}/{newPhoto.DisplayName}/");
        }

        public async Task<IActionResult> OnPostDelete(string albumName, string photoName)
        {
            if (RequireAdmin() is { } challenge)
            {
                return challenge;
            }

            Photo = FindPhoto(albumName, photoName);

            if (Photo == null)
            {
                return NotFound();
            }

            var album = Photo.Album;

            await _store.DeletePhotoAsync(album.Id, Photo.Id);
            album.RemovePhoto(Photo);

            return new RedirectResult($"~/album/{WebUtility.UrlEncode(albumName).Replace('+', ' ')}/");
        }

        private Photo FindPhoto(string albumName, string photoName)
        {
            var album = _ac.Albums.FirstOrDefault(a => a.Id.Equals(albumName, StringComparison.OrdinalIgnoreCase));
            return album?.Photos.FirstOrDefault(p => p.DisplayName.Equals(photoName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
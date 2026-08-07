using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GlaaTrips.Models
{
    /// <summary>
    /// An <see cref="IPhotoStore"/> backed by an Azure Blob Storage container.
    /// Album content is stored under the same key layout the local store uses
    /// (<c>{albumId}/data.json</c>, <c>{albumId}/{photo}</c>,
    /// <c>{albumId}/thumbnail/{thumb}</c>, and a top-level
    /// <c>markers.json</c>), so content is decoupled from the app and survives
    /// redeploys. Photos are served directly from the container — or a CDN in
    /// front of it — via the public URLs this store returns, rather than being
    /// proxied through the app. The container is created on start-up with
    /// blob-level public read access so those URLs resolve.
    /// </summary>
    public sealed class AzureBlobPhotoStore : IPhotoStore
    {
        private readonly BlobContainerClient _container;
        private readonly string _publicBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobPhotoStore"/>
        /// class and ensures the backing container exists.
        /// </summary>
        /// <param name="connectionString">The storage account connection string.</param>
        /// <param name="containerName">The container album content is stored in.</param>
        /// <param name="publicBaseUrl">The public base URL content is served from (for example a CDN endpoint); when empty the container's own URL is used.</param>
        public AzureBlobPhotoStore(string connectionString, string containerName, string publicBaseUrl)
        {
            var service = new BlobServiceClient(connectionString);
            _container = service.GetBlobContainerClient(containerName);
            _container.CreateIfNotExists(PublicAccessType.Blob);

            _publicBase = string.IsNullOrWhiteSpace(publicBaseUrl)
                ? _container.Uri.ToString().TrimEnd('/')
                : publicBaseUrl.TrimEnd('/');
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ListAlbumIds()
        {
            var ids = new List<string>();

            foreach (var item in _container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", null, default))
            {
                if (item.IsPrefix)
                {
                    ids.Add(item.Prefix.TrimEnd('/'));
                }
            }

            return ids;
        }

        /// <inheritdoc />
        public AlbumMetaData TryReadMetadata(string albumId)
        {
            var blob = _container.GetBlobClient(MetadataKey(albumId));

            if (!blob.Exists())
            {
                return null;
            }

            BlobDownloadResult download = blob.DownloadContent();
            return JsonSerializer.Deserialize<AlbumMetaData>(download.Content.ToString());
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ListPhotoFileNames(string albumId)
        {
            string prefix = albumId + "/";
            var names = new List<string>();

            // A hierarchical listing returns the blobs directly under the album
            // (data.json and the photos) and a prefix for the thumbnail folder;
            // taking the blobs and keeping the image files yields the originals.
            foreach (var item in _container.GetBlobsByHierarchy(BlobTraits.None, BlobStates.None, "/", prefix, default))
            {
                if (item.IsBlob)
                {
                    string name = item.Blob.Name.Substring(prefix.Length);
                    if (PhotoStoreConventions.IsImageFile(name))
                    {
                        names.Add(name);
                    }
                }
            }

            return names;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ListThumbnailFileNames(string albumId)
        {
            string prefix = ThumbnailPrefix(albumId);

            return _container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, default)
                .Select(b => b.Name.Substring(prefix.Length))
                .ToList();
        }

        /// <inheritdoc />
        public bool AlbumExists(string albumId)
        {
            return _container.GetBlobs(BlobTraits.None, BlobStates.None, albumId + "/", default).Any();
        }

        /// <inheritdoc />
        public bool PhotoExists(string albumId, string fileName)
        {
            return _container.GetBlobClient(PhotoKey(albumId, fileName)).Exists();
        }

        /// <inheritdoc />
        public async Task WriteMetadataAsync(string albumId, AlbumMetaData metadata)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, metadata);
            stream.Position = 0;
            await _container.GetBlobClient(MetadataKey(albumId)).UploadAsync(stream, overwrite: true);
        }

        /// <inheritdoc />
        public async Task DeleteAlbumAsync(string albumId)
        {
            foreach (var blob in _container.GetBlobs(BlobTraits.None, BlobStates.None, albumId + "/", default).ToList())
            {
                await _container.GetBlobClient(blob.Name).DeleteIfExistsAsync();
            }
        }

        /// <inheritdoc />
        public async Task SavePhotoAsync(string albumId, string fileName, Stream content)
        {
            await _container.GetBlobClient(PhotoKey(albumId, fileName)).UploadAsync(content, overwrite: true);
        }

        /// <inheritdoc />
        public Stream OpenPhoto(string albumId, string fileName)
        {
            return _container.GetBlobClient(PhotoKey(albumId, fileName)).OpenRead();
        }

        /// <inheritdoc />
        public async Task SaveThumbnailAsync(string albumId, string thumbnailFileName, Stream content)
        {
            await _container.GetBlobClient(ThumbnailKey(albumId, thumbnailFileName)).UploadAsync(content, overwrite: true);
        }

        /// <inheritdoc />
        public async Task DeletePhotoAsync(string albumId, string fileName)
        {
            await _container.GetBlobClient(PhotoKey(albumId, fileName)).DeleteIfExistsAsync();

            string prefix = ThumbnailPrefix(albumId);

            foreach (var blob in _container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, default).ToList())
            {
                string name = blob.Name.Substring(prefix.Length);
                if (PhotoStoreConventions.ThumbnailBelongsTo(name, fileName))
                {
                    await _container.GetBlobClient(blob.Name).DeleteIfExistsAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task RenamePhotoAsync(string albumId, string oldFileName, string newFileName)
        {
            await CopyBlobAsync(PhotoKey(albumId, oldFileName), PhotoKey(albumId, newFileName));
            await _container.GetBlobClient(PhotoKey(albumId, oldFileName)).DeleteIfExistsAsync();

            string prefix = ThumbnailPrefix(albumId);

            foreach (var blob in _container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, default).ToList())
            {
                string name = blob.Name.Substring(prefix.Length);
                if (PhotoStoreConventions.ThumbnailBelongsTo(name, oldFileName))
                {
                    string renamed = PhotoStoreConventions.RenameThumbnail(name, oldFileName, newFileName);
                    await CopyBlobAsync(blob.Name, prefix + renamed);
                    await _container.GetBlobClient(blob.Name).DeleteIfExistsAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task WriteMarkersAsync(IEnumerable<Marker> markers)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, markers);
            stream.Position = 0;
            await _container.GetBlobClient(PhotoStoreConventions.MarkersFileName).UploadAsync(stream, overwrite: true);
        }

        /// <inheritdoc />
        public string PhotoUrl(string albumId, string fileName)
        {
            return $"{_publicBase}/{Escape(albumId)}/{Escape(fileName)}";
        }

        /// <inheritdoc />
        public string ThumbnailUrl(string albumId, string thumbnailFileName)
        {
            return $"{_publicBase}/{Escape(albumId)}/{PhotoStoreConventions.ThumbnailFolder}/{Escape(thumbnailFileName)}";
        }

        /// <inheritdoc />
        public string MarkersUrl()
        {
            return $"{_publicBase}/{PhotoStoreConventions.MarkersFileName}";
        }

        private static string Escape(string segment)
        {
            return Uri.EscapeDataString(segment);
        }

        private static string MetadataKey(string albumId)
        {
            return $"{albumId}/{PhotoStoreConventions.MetadataFileName}";
        }

        private static string PhotoKey(string albumId, string fileName)
        {
            return $"{albumId}/{fileName}";
        }

        private static string ThumbnailPrefix(string albumId)
        {
            return $"{albumId}/{PhotoStoreConventions.ThumbnailFolder}/";
        }

        private static string ThumbnailKey(string albumId, string thumbnailFileName)
        {
            return $"{albumId}/{PhotoStoreConventions.ThumbnailFolder}/{thumbnailFileName}";
        }

        private async Task CopyBlobAsync(string sourceKey, string destinationKey)
        {
            using var stream = await _container.GetBlobClient(sourceKey).OpenReadAsync();
            await _container.GetBlobClient(destinationKey).UploadAsync(stream, overwrite: true);
        }
    }
}
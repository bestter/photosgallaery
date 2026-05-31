using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PhotoAppApi.Models;

namespace PhotoAppApi.Services
{
    public interface IObjectStorageService
    {
        Task<string> UploadImageAsync(Stream fileStream, string contentType, string fileName, string? folder = "images", CancellationToken cancellationToken = default);
        Task<string> GetPresignedUrlAsync(string key, TimeSpan? expiration = null);

        /// <summary>
        /// Deletes an image from the object storage.
        /// </summary>
        Task DeleteImageAsync(string key, CancellationToken cancellationToken = default);
    }

    public class ObjectStorageService : IObjectStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public ObjectStorageService(IAmazonS3 s3Client, IOptions<ObjectStorageOptions> options)
        {
            _s3Client = s3Client;
            _bucketName = options.Value.BucketName;
        }

        /// <summary>
        /// Deletes an image from the S3 bucket using its key.
        /// </summary>
        public async Task DeleteImageAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key)) return;

            var safeKey = key.TrimStart('/');
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = safeKey
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string contentType, string fileName, string? folder = "images", CancellationToken cancellationToken = default)
        {
            if (fileStream == null || fileStream.Length == 0) throw new ArgumentException("Stream vide");

            var s3FileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var key = string.IsNullOrEmpty(folder) ? s3FileName : $"{folder}/{s3FileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                DisablePayloadSigning = true,
                Metadata =
            {
                ["original-filename"] = fileName,
                ["uploaded-at"] = DateTime.UtcNow.ToString("o")
            }
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            // Retourne l’URL publique (si le bucket est public) ou la clé
            return key; // ou $"https://TON_CDN/{key}" si tu utilises un CDN
        }

        // === VERSION RECOMMANDÉE : URL Présignée ===
        public async Task<string> GetPresignedUrlAsync(string key, TimeSpan? expiration = null)
        {
            var safeKey = key.TrimStart('/');
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = safeKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(15))
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PhotoAppApi.Models;

namespace PhotoAppApi.Services
{
    public interface IObjectStorageService
    {
        Task<string> UploadImageAsync(IFormFile file, string fileName, string? folder = "images", CancellationToken cancellationToken = default);
        Task<string> GetPresignedUrlAsync(string key, TimeSpan? expiration = null);
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

        public async Task<string> UploadImageAsync(IFormFile file, string fileName, string? folder = "images", CancellationToken cancellationToken = default)
        {
            if (file.Length == 0) throw new ArgumentException("Fichier vide");

            var s3FileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var key = string.IsNullOrEmpty(folder) ? s3FileName : $"{folder}/{s3FileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType,
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
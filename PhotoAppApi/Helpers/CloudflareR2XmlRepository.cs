using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;

namespace PhotoAppApi.Helpers
{
    public class CloudflareR2XmlRepository : IDeletableXmlRepository
    {

        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _prefix;
        private static readonly ILog log = LogManager.GetLogger(typeof(CloudflareR2XmlRepository));

        public CloudflareR2XmlRepository(
            IAmazonS3 s3Client,
            string bucketName,
            string prefix = "dataprotection-keys/")
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _bucketName = !string.IsNullOrWhiteSpace(bucketName)
                ? bucketName
                : throw new ArgumentException("Bucket name cannot be null or empty.", nameof(bucketName));
            _prefix = prefix ?? "dataprotection-keys/";
        }

        private class DeletableElement : IDeletableElement
        {
            public XElement Element { get; }
            public bool ShouldDelete { get; set; }
            public int? DeletionOrder { get; set; }   // ← auto-property correcte

            public DeletableElement(XElement element, int? deletionOrder = null)
            {
                Element = element ?? throw new ArgumentNullException(nameof(element));
                DeletionOrder = deletionOrder;
                ShouldDelete = false;   // ← important : le framework décide
            }
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            // ⚡ Bolt: Removed Task.Run wrapper to prevent thread pool starvation when making blocking async calls.
            return GetAllElementsAsync().GetAwaiter().GetResult();
        }

        private async Task<IReadOnlyCollection<XElement>> GetAllElementsAsync(CancellationToken ct = default)
        {
            var elements = new List<XElement>();
            string? continuationToken = null;

            do
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = _prefix,
                    ContinuationToken = continuationToken
                };

                var response = await _s3Client.ListObjectsV2Async(request, ct);


                var s3Objects = (response?.S3Objects ?? Enumerable.Empty<S3Object>())
                    .Where(o => o?.Key?.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                // ⚡ Bolt: Replace unbounded Task.WhenAll with Parallel.ForEachAsync for bounded concurrency
                var results = new XElement?[s3Objects.Count];
                var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct };

                await Parallel.ForEachAsync(Enumerable.Range(0, s3Objects.Count), options, async (i, token) =>
                {
                    var obj = s3Objects[i];
                    try
                    {
                        var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = obj.Key };
                        using var getResponse = await _s3Client.GetObjectAsync(getRequest, token);
                        using var streamReader = new StreamReader(getResponse.ResponseStream);

                        results[i] = await XElement.LoadAsync(streamReader, LoadOptions.None, token);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error loading Data Protection key from R2: {obj?.Key}", ex);
                        results[i] = null;
                    }
                });

                elements.AddRange(results.Where(x => x != null)!);

                continuationToken = response.NextContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            return elements.AsReadOnly();
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            try
            {
                // ⚡ Bolt: Removed Task.Run wrapper to prevent thread pool starvation when making blocking async calls.
                StoreElementAsync(element, friendlyName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                log.Error($"Error storing Data Protection element {friendlyName}", ex);
                throw;
            }
        }

        private async Task StoreElementAsync(XElement element, string friendlyName, CancellationToken cancellationToken = default)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(friendlyName)) throw new ArgumentException("Friendly name cannot be null or empty.", nameof(friendlyName));

            var key = $"{_prefix}{friendlyName}.xml";

            using var memoryStream = new MemoryStream();

            await element.SaveAsync(memoryStream, SaveOptions.DisableFormatting, cancellationToken);
            memoryStream.Position = 0;

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = memoryStream,
                ContentType = "application/xml",
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                log.Error($"Failed to store element {friendlyName}. Status: {response.HttpStatusCode}");
                throw new InvalidOperationException($"Failed to store element. HTTP {response.HttpStatusCode}");
            }
        }

        public bool DeleteElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements)
        {
            try
            {
                // ⚡ Bolt: Removed Task.Run wrapper to prevent thread pool starvation when making blocking async calls.
                return DeleteElementsAsync(chooseElements).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                log.Error("Error during DeleteElements operation", ex);
                throw;
            }
        }

        private async Task<bool> DeleteElementsAsync(Action<IReadOnlyCollection<IDeletableElement>> chooseElements, CancellationToken cancellationToken = default)
        {
            if (chooseElements == null) throw new ArgumentNullException(nameof(chooseElements));

            var allElements = await GetAllElementsAsync(cancellationToken);

            // Création simple et efficace
            var deletableList = allElements
                .Select((e, i) => new DeletableElement(e, i))
                .ToList();

            // Le système Data Protection va remplir ShouldDelete + DeletionOrder
            chooseElements(deletableList.AsReadOnly());

            bool success = true;

            var keysToDelete = deletableList
                .Where(d => d.ShouldDelete)
                .Select(d => d.Element.Attribute("friendlyName")?.Value)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new KeyVersion { Key = $"{_prefix}{name}.xml" })
                .ToList();

            // ⚡ Bolt: Use .Count == 0 instead of .Any() on a List to avoid enumerator allocation overhead
            if (keysToDelete.Count == 0)
                return success;

            // AWS S3 DeleteObjects allows up to 1000 keys per request
            for (int i = 0; i < keysToDelete.Count; i += 1000)
            {
                var batch = keysToDelete.Skip(i).Take(1000).ToList();
                try
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = batch
                    };

                    var response = await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);

                    // ⚡ Bolt: Use .Count != 0 instead of .Any() on a List to avoid enumerator allocation overhead
                    if (response.DeleteErrors != null && response.DeleteErrors.Count != 0)
                    {
                        foreach (var error in response.DeleteErrors)
                        {
                            log.Warn($"Failed to delete key {error.Key}. Code: {error.Code}, Message: {error.Message}");
                        }
                        success = false;
                    }

                    // ⚡ Bolt: Use .Count != 0 instead of .Any() on a List to avoid enumerator allocation overhead
                    if (response.DeletedObjects != null && response.DeletedObjects.Count != 0)
                    {
                        foreach (var deleted in response.DeletedObjects)
                        {
                            log.Info($"Successfully deleted old Data Protection key: {deleted.Key}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Exception while deleting batch of keys", ex);
                    success = false;
                }
            }

            return success;
        }
    }
}

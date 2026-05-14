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
            return Task.Run(() => GetAllElementsAsync()).GetAwaiter().GetResult();
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


                var getTasks = (response?.S3Objects ?? Enumerable.Empty<S3Object>())
                    .Where(o => o?.Key?.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(async obj =>
                    {
                        try
                        {
                            var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = obj.Key };
                            using var getResponse = await _s3Client.GetObjectAsync(getRequest, ct);
                            using var streamReader = new StreamReader(getResponse.ResponseStream);

                            return await XElement.LoadAsync(streamReader, LoadOptions.None, ct);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error loading Data Protection key from R2: {obj?.Key}", ex);
                            return null;
                        }
                    });

                var results = await Task.WhenAll(getTasks);
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
                Task.Run(() => StoreElementAsync(element, friendlyName)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                log.Error( $"Error storing Data Protection element {friendlyName}", ex);
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
                return Task.Run(() => DeleteElementsAsync(chooseElements)).GetAwaiter().GetResult();
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

            foreach (var deletable in deletableList.Where(d => d.ShouldDelete))
            {
                var friendlyName = deletable.Element.Attribute("friendlyName")?.Value;
                if (string.IsNullOrEmpty(friendlyName))
                    continue;

                var key = $"{_prefix}{friendlyName}.xml";

                try
                {
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key
                    };

                    var response = await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

                    if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
                    {
                        log.Warn($"Failed to delete key {key}. Status: {response.HttpStatusCode}");
                        success = false;
                    }
                    else
                    {
                        log.Info($"Successfully deleted old Data Protection key: {key}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Exception while deleting key {key}", ex);
                    success = false;
                }
            }

            return success;
        }
    }
}
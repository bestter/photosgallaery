using System.Xml.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.Repositories;

public class CloudflareR2XmlRepository : IXmlRepository
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _prefix;

    public CloudflareR2XmlRepository(IAmazonS3 s3Client, string bucketName, string prefix = "dataprotection-keys/")
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _prefix = prefix;
    }

    // Méthode 1 : Lire les clés au démarrage
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using (var tokenSource2 = new CancellationTokenSource())
        {
            CancellationToken ct = tokenSource2.Token;
            // IXmlRepository est synchrone par design dans ASP.NET, on doit donc bloquer proprement
            return Task.Run(async () => {
                // Were we already canceled?
                ct.ThrowIfCancellationRequested();
                return await GetAllElementsAsync(ct); }).GetAwaiter().GetResult();
        }
    }

    private async Task<IReadOnlyCollection<XElement>> GetAllElementsAsync(CancellationToken cancellationToken = default)
    {
        var elements = new List<XElement>();
        var request = new ListObjectsV2Request { BucketName = _bucketName, Prefix = _prefix };

        var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

        // FIX : On s'assure que S3Objects n'est pas null avant de boucler.
        // L'opérateur de fusion null (??) renvoie une liste vide si Cloudflare omet la propriété.
        var s3Objects = response?.S3Objects ?? new List<S3Object>();

        foreach (var s3Object in s3Objects)
        {
            var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = s3Object.Key };
            using var getResponse = await _s3Client.GetObjectAsync(getRequest, cancellationToken);
            using var streamReader = new StreamReader(getResponse.ResponseStream);

            // On parse le fichier S3 en XML
            elements.Add(await XElement.LoadAsync(streamReader, LoadOptions.None, cancellationToken));
        }

        return elements.AsReadOnly();
    }

    // Méthode 2 : Sauvegarder une nouvelle clé
    public void StoreElement(XElement element, string friendlyName)
    {
        using (var tokenSource2 = new CancellationTokenSource())
        {
            CancellationToken ct = tokenSource2.Token;
            Task.Run(async () =>
            {
                // Were we already canceled?
                ct.ThrowIfCancellationRequested();
                await StoreElementAsync(element, friendlyName, ct);

            }).GetAwaiter().GetResult();
        }
    }

    private async Task StoreElementAsync(XElement element, string friendlyName, CancellationToken cancellationToken = default)
    {
        var key = $"{_prefix}{friendlyName}.xml";

        using var memoryStream = new MemoryStream();
        await element.SaveAsync(memoryStream, SaveOptions.DisableFormatting, cancellationToken);
        memoryStream.Position = 0; // On remet le curseur au début pour la lecture

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = memoryStream,
            ContentType = "application/xml",
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
    }
}
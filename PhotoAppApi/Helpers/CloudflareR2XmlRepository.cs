using log4net;
﻿using System.Xml.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.DataProtection.Repositories;

public class CloudflareR2XmlRepository : IXmlRepository
{
        private static readonly ILog log = LogManager.GetLogger(typeof(CloudflareR2XmlRepository));

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

    private async Task<IReadOnlyCollection<XElement>> GetAllElementsAsync(CancellationToken ct = default)
    {
        var elements = new List<XElement>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request { BucketName = _bucketName, Prefix = _prefix, ContinuationToken = continuationToken };
            var response = await _s3Client.ListObjectsV2Async(request, ct);

            var getTasks = response.S3Objects
                .Where(o => o?.Key?.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) == true)
                .Select(async obj =>
                {
                    try
                    {   
                            var getRequest = new GetObjectRequest { BucketName = _bucketName, Key = obj.Key };
                            using var getResponse = await _s3Client.GetObjectAsync(getRequest, ct);
                            using var streamReader = new StreamReader(getResponse.ResponseStream);

                        // On parse le fichier S3 en XML
                        return await XElement.LoadAsync(streamReader, LoadOptions.None, ct);
                        
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error occurred while processing object: {obj?.Key}", ex);
                        // log + return null (on ignore les clés corrompues)
                        return null;
                    }
                });

            var results = await Task.WhenAll(getTasks);
            elements.AddRange(results.Where(x => x != null)!);

            continuationToken = response.NextContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return elements.AsReadOnly();
    }

    // Méthode 2 : Sauvegarder une nouvelle clé
    public void StoreElement(XElement element, string friendlyName)
    {
        try
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
        catch (Exception ex)
        {
            log.Error($"Error occurred while storing element with friendly name: {friendlyName}", ex);
            throw; // On rethrow pour que l'appelant puisse gérer l'erreur
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
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,   // ← à ajouter                                                       
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256  // optionnel mais recommandé
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);
    }
}
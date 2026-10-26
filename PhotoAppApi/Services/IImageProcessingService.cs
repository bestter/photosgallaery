using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoAppApi.Services
{
    public record ImageMetadataInfo(
        DateTime? DateTaken,
        string? CameraModel,
        double? Latitude,
        double? Longitude
    );

    public sealed record ProcessedImageResult(
        Stream MainStream,
        Stream ThumbnailStream,
        int NormalizedWidth,
        int NormalizedHeight,
        int ThumbnailWidth,
        int ThumbnailHeight,
        string Extension,
        string ContentType,
        ImageMetadataInfo Metadata
    ) : IDisposable
    {
        public void Dispose()
        {
            MainStream.Dispose();
            ThumbnailStream.Dispose();
        }
    }

    public sealed record ThumbnailOnlyResult(
        Stream ThumbnailStream,
        int ThumbnailWidth,
        int ThumbnailHeight,
        string Extension,
        string ContentType
    ) : IDisposable
    {
        public void Dispose()
        {
            ThumbnailStream.Dispose();
        }
    }

    public interface IImageProcessingService
    {
        ImageInspectionResult Inspect(Stream inputStream);
        Task<ProcessedImageResult> ProcessImageAsync(Stream inputStream, bool includeGps, CancellationToken cancellationToken = default);
        Task<ThumbnailOnlyResult> GenerateThumbnailAsync(Stream inputStream, CancellationToken cancellationToken = default);
    }
}

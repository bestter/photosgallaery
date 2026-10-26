using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SkiaSharp;

namespace PhotoAppApi.Services
{
    public class ImageProcessingService : IImageProcessingService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ImageProcessingService));

        public ImageInspectionResult Inspect(Stream inputStream)
        {
            return ImageFormatInspector.Inspect(inputStream);
        }

        public Task<ProcessedImageResult> ProcessImageAsync(Stream inputStream, bool includeGps, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inspection = Inspect(inputStream);
            if (!inspection.IsValid)
            {
                throw new InvalidOperationException(inspection.ErrorMessage ?? "Image non supportée ou invalide.");
            }

            long originalPos = inputStream.CanSeek ? inputStream.Position : 0;

            // 1. Extract metadata before stripping
            ImageMetadataInfo metadata = ExtractMetadata(inputStream, includeGps);

            // 2. Decode, normalize orientation and color space
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            using var managedStream = new SKManagedStream(inputStream, false);
            using var codec = SKCodec.Create(managedStream);
            if (codec == null)
            {
                throw new InvalidOperationException("Impossible de décoder le fichier image.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var origin = codec.EncodedOrigin;
            int rawWidth = codec.Info.Width;
            int rawHeight = codec.Info.Height;

            var decodeInfo = new SKImageInfo(
                rawWidth,
                rawHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Unpremul,
                SKColorSpace.CreateSrgb()
            );

            using var rawBitmap = new SKBitmap(decodeInfo);
            var codecResult = codec.GetPixels(decodeInfo, rawBitmap.GetPixels());
            if (codecResult != SKCodecResult.Success)
            {
                throw new InvalidOperationException("Échec du décodage de l'image (données corrompues ou incomplètes).");
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool swapDimensions = origin is SKEncodedOrigin.LeftTop
                or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom
                or SKEncodedOrigin.LeftBottom;

            int normalizedWidth = swapDimensions ? rawHeight : rawWidth;
            int normalizedHeight = swapDimensions ? rawWidth : rawHeight;

            var normalizedInfo = new SKImageInfo(
                normalizedWidth,
                normalizedHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Premul,
                SKColorSpace.CreateSrgb()
            );

            using var normalizedSurface = SKSurface.Create(normalizedInfo);
            if (normalizedSurface == null)
            {
                throw new InvalidOperationException("Impossible d'allouer la surface de rendu de l'image normalisée.");
            }

            var canvas = normalizedSurface.Canvas;
            canvas.SetMatrix(GetOrientationMatrix(origin, rawWidth, rawHeight));

            using var rawImage = SKImage.FromBitmap(rawBitmap);
            canvas.DrawImage(rawImage, 0, 0);
            canvas.Flush();

            using var normalizedImage = normalizedSurface.Snapshot();

            cancellationToken.ThrowIfCancellationRequested();

            // 3. Compute thumbnail dimensions and render with Mitchell cubic resampler
            double scale = Math.Min(1.0, Math.Min(400.0 / normalizedWidth, 400.0 / normalizedHeight));
            int thumbWidth = scale >= 1.0
                ? normalizedWidth
                : Math.Clamp((int)Math.Round(normalizedWidth * scale, MidpointRounding.AwayFromZero), 1, 400);
            int thumbHeight = scale >= 1.0
                ? normalizedHeight
                : Math.Clamp((int)Math.Round(normalizedHeight * scale, MidpointRounding.AwayFromZero), 1, 400);

            var thumbInfo = new SKImageInfo(
                thumbWidth,
                thumbHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Premul,
                SKColorSpace.CreateSrgb()
            );

            using var thumbSurface = SKSurface.Create(thumbInfo);
            if (thumbSurface == null)
            {
                throw new InvalidOperationException("Impossible d'allouer la surface de rendu de la miniature.");
            }

            var thumbCanvas = thumbSurface.Canvas;
            var destRect = new SKRect(0, 0, thumbWidth, thumbHeight);
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
            using var paint = new SKPaint();
            thumbCanvas.DrawImage(normalizedImage, destRect, sampling, paint);
            thumbCanvas.Flush();

            using var thumbImage = thumbSurface.Snapshot();

            cancellationToken.ThrowIfCancellationRequested();

            // 4. Encode both outputs to fresh MemoryStreams
            var (format, quality) = GetEncoderParameters(inspection.Extension!);

            using var mainData = normalizedImage.Encode(format, quality);
            if (mainData == null)
            {
                throw new InvalidOperationException("Échec de l'encodage de l'image principale.");
            }

            var mainStream = new MemoryStream();
            mainData.SaveTo(mainStream);
            mainStream.Position = 0;

            using var thumbData = thumbImage.Encode(format, quality);
            if (thumbData == null)
            {
                mainStream.Dispose();
                throw new InvalidOperationException("Échec de l'encodage de la miniature.");
            }

            var thumbStream = new MemoryStream();
            thumbData.SaveTo(thumbStream);
            thumbStream.Position = 0;

            if (inputStream.CanSeek)
            {
                inputStream.Position = originalPos;
            }

            return Task.FromResult(new ProcessedImageResult(
                mainStream,
                thumbStream,
                normalizedWidth,
                normalizedHeight,
                thumbWidth,
                thumbHeight,
                inspection.Extension!,
                inspection.ContentType!,
                metadata
            ));
        }

        public Task<ThumbnailOnlyResult> GenerateThumbnailAsync(Stream inputStream, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var inspection = Inspect(inputStream);
            if (!inspection.IsValid)
            {
                throw new InvalidOperationException(inspection.ErrorMessage ?? "Image non supportée ou invalide.");
            }

            long originalPos = inputStream.CanSeek ? inputStream.Position : 0;
            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            using var managedStream = new SKManagedStream(inputStream, false);
            using var codec = SKCodec.Create(managedStream);
            if (codec == null)
            {
                throw new InvalidOperationException("Impossible de décoder le fichier image.");
            }

            var origin = codec.EncodedOrigin;
            int rawWidth = codec.Info.Width;
            int rawHeight = codec.Info.Height;

            var decodeInfo = new SKImageInfo(
                rawWidth,
                rawHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Unpremul,
                SKColorSpace.CreateSrgb()
            );

            using var rawBitmap = new SKBitmap(decodeInfo);
            var codecResult = codec.GetPixels(decodeInfo, rawBitmap.GetPixels());
            if (codecResult != SKCodecResult.Success)
            {
                throw new InvalidOperationException("Échec du décodage de l'image.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            bool swapDimensions = origin is SKEncodedOrigin.LeftTop
                or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom
                or SKEncodedOrigin.LeftBottom;

            int normalizedWidth = swapDimensions ? rawHeight : rawWidth;
            int normalizedHeight = swapDimensions ? rawWidth : rawHeight;

            var normalizedInfo = new SKImageInfo(
                normalizedWidth,
                normalizedHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Premul,
                SKColorSpace.CreateSrgb()
            );

            using var normalizedSurface = SKSurface.Create(normalizedInfo);
            var canvas = normalizedSurface.Canvas;
            canvas.SetMatrix(GetOrientationMatrix(origin, rawWidth, rawHeight));

            using var rawImage = SKImage.FromBitmap(rawBitmap);
            canvas.DrawImage(rawImage, 0, 0);
            canvas.Flush();

            using var normalizedImage = normalizedSurface.Snapshot();

            cancellationToken.ThrowIfCancellationRequested();

            double scale = Math.Min(1.0, Math.Min(400.0 / normalizedWidth, 400.0 / normalizedHeight));
            int thumbWidth = scale >= 1.0
                ? normalizedWidth
                : Math.Clamp((int)Math.Round(normalizedWidth * scale, MidpointRounding.AwayFromZero), 1, 400);
            int thumbHeight = scale >= 1.0
                ? normalizedHeight
                : Math.Clamp((int)Math.Round(normalizedHeight * scale, MidpointRounding.AwayFromZero), 1, 400);

            var thumbInfo = new SKImageInfo(
                thumbWidth,
                thumbHeight,
                SKColorType.Rgba8888,
                SKAlphaType.Premul,
                SKColorSpace.CreateSrgb()
            );

            using var thumbSurface = SKSurface.Create(thumbInfo);
            var thumbCanvas = thumbSurface.Canvas;
            var destRect = new SKRect(0, 0, thumbWidth, thumbHeight);
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
            using var paint = new SKPaint();
            thumbCanvas.DrawImage(normalizedImage, destRect, sampling, paint);
            thumbCanvas.Flush();

            using var thumbImage = thumbSurface.Snapshot();

            cancellationToken.ThrowIfCancellationRequested();

            var (format, quality) = GetEncoderParameters(inspection.Extension!);
            using var thumbData = thumbImage.Encode(format, quality);
            if (thumbData == null)
            {
                throw new InvalidOperationException("Échec de l'encodage de la miniature.");
            }

            var thumbStream = new MemoryStream();
            thumbData.SaveTo(thumbStream);
            thumbStream.Position = 0;

            if (inputStream.CanSeek)
            {
                inputStream.Position = originalPos;
            }

            return Task.FromResult(new ThumbnailOnlyResult(
                thumbStream,
                thumbWidth,
                thumbHeight,
                inspection.Extension!,
                inspection.ContentType!
            ));
        }

        private static (SKEncodedImageFormat Format, int Quality) GetEncoderParameters(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".png" => (SKEncodedImageFormat.Png, 100),
                ".webp" => (SKEncodedImageFormat.Webp, 90),
                _ => (SKEncodedImageFormat.Jpeg, 90)
            };
        }

        private static SKMatrix GetOrientationMatrix(SKEncodedOrigin origin, int width, int height)
        {
            return origin switch
            {
                SKEncodedOrigin.TopLeft => new SKMatrix
                {
                    ScaleX = 1, SkewX = 0, TransX = 0,
                    SkewY = 0, ScaleY = 1, TransY = 0,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.TopRight => new SKMatrix // Flip Horizontal
                {
                    ScaleX = -1, SkewX = 0, TransX = width,
                    SkewY = 0, ScaleY = 1, TransY = 0,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.BottomRight => new SKMatrix // Rotate 180
                {
                    ScaleX = -1, SkewX = 0, TransX = width,
                    SkewY = 0, ScaleY = -1, TransY = height,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.BottomLeft => new SKMatrix // Flip Vertical
                {
                    ScaleX = 1, SkewX = 0, TransX = 0,
                    SkewY = 0, ScaleY = -1, TransY = height,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.LeftTop => new SKMatrix // Transpose: x'=y, y'=x
                {
                    ScaleX = 0, SkewX = 1, TransX = 0,
                    SkewY = 1, ScaleY = 0, TransY = 0,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.RightTop => new SKMatrix // Rotate 90 CW: x'=H-y, y'=x
                {
                    ScaleX = 0, SkewX = -1, TransX = height,
                    SkewY = 1, ScaleY = 0, TransY = 0,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.RightBottom => new SKMatrix // Flip H then Rotate 90 CW: x'=H-y, y'=W-x
                {
                    ScaleX = 0, SkewX = -1, TransX = height,
                    SkewY = -1, ScaleY = 0, TransY = width,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                SKEncodedOrigin.LeftBottom => new SKMatrix // Rotate 270 CW: x'=y, y'=W-x
                {
                    ScaleX = 0, SkewX = 1, TransX = 0,
                    SkewY = -1, ScaleY = 0, TransY = width,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                },
                _ => new SKMatrix
                {
                    ScaleX = 1, SkewX = 0, TransX = 0,
                    SkewY = 0, ScaleY = 1, TransY = 0,
                    Persp0 = 0, Persp1 = 0, Persp2 = 1
                }
            };
        }

        private static ImageMetadataInfo ExtractMetadata(Stream stream, bool includeGps)
        {
            DateTime? dateTaken = null;
            string? cameraModel = null;
            double? latitude = null;
            double? longitude = null;

            try
            {
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                }

                var directories = ImageMetadataReader.ReadMetadata(stream);

                // Date Taken
                var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (subIfd != null && subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt))
                {
                    dateTaken = dt;
                }
                else if (subIfd != null)
                {
                    string? dtStr = subIfd.GetString(ExifDirectoryBase.TagDateTimeOriginal);
                    if (!string.IsNullOrWhiteSpace(dtStr) &&
                        DateTime.TryParseExact(dtStr, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
                    {
                        dateTaken = parsedDt;
                    }
                }

                // Camera Model
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (ifd0 != null)
                {
                    string? model = ifd0.GetString(ExifDirectoryBase.TagModel);
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        string trimmed = model.Trim('\0', ' ');
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            cameraModel = trimmed;
                        }
                    }
                }

                // GPS
                if (includeGps)
                {
                    var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
                    if (gpsDir != null)
                    {
                        latitude = ExtractCoordinateSafely(
                            gpsDir.GetRationalArray(GpsDirectory.TagLatitude),
                            gpsDir.GetString(GpsDirectory.TagLatitudeRef),
                            isLatitude: true
                        );

                        longitude = ExtractCoordinateSafely(
                            gpsDir.GetRationalArray(GpsDirectory.TagLongitude),
                            gpsDir.GetString(GpsDirectory.TagLongitudeRef),
                            isLatitude: false
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn("Erreur lors de l'extraction des métadonnées d'une image.", ex);
            }

            return new ImageMetadataInfo(dateTaken, cameraModel, latitude, longitude);
        }

        private static double? ExtractCoordinateSafely(MetadataExtractor.Rational[]? rationals, string? reference, bool isLatitude)
        {
            if (rationals == null || rationals.Length < 3 || string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            if (rationals[0].Denominator == 0 || rationals[1].Denominator == 0 || rationals[2].Denominator == 0)
            {
                return null;
            }

            double degrees = (double)rationals[0].Numerator / rationals[0].Denominator;
            double minutes = (double)rationals[1].Numerator / rationals[1].Denominator;
            double seconds = (double)rationals[2].Numerator / rationals[2].Denominator;

            double result = degrees + (minutes / 60.0) + (seconds / 3600.0);
            if (!double.IsFinite(result))
            {
                return null;
            }

            string cleanRef = reference.Trim().ToUpperInvariant();
            if (isLatitude)
            {
                if (cleanRef == "S")
                {
                    result = -result;
                }
                else if (cleanRef != "N")
                {
                    return null;
                }

                if (result < -90.0 || result > 90.0)
                {
                    return null;
                }
            }
            else
            {
                if (cleanRef == "W")
                {
                    result = -result;
                }
                else if (cleanRef != "E")
                {
                    return null;
                }

                if (result < -180.0 || result > 180.0)
                {
                    return null;
                }
            }

            return Math.Round(result, 6);
        }
    }
}

using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using SkiaSharp;

namespace PhotoAppApi.Services;

public record ImageInspectionResult(
    bool IsValid,
    string? Extension,
    string? ContentType,
    int Width,
    int Height,
    long PixelCount,
    bool IsAnimated,
    string? ErrorMessage
);

public static class ImageFormatInspector
{
    public const long MaxPixelCount = 20_000_000;

    public static ImageInspectionResult Inspect(Stream stream)
    {
        if (stream == null || !stream.CanRead)
        {
            return new ImageInspectionResult(false, null, null, 0, 0, 0, false, "Le flux de données de l'image est indisponible.");
        }

        var originalPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            if (stream.Length < 12)
            {
                return new ImageInspectionResult(false, null, null, 0, 0, 0, false, "Le fichier est trop petit pour être une image valide.");
            }

            var header = new byte[12];
            var read = stream.Read(header, 0, 12);
            if (read < 12)
            {
                return new ImageInspectionResult(false, null, null, 0, 0, 0, false, "Impossible de lire l'en-tête de l'image.");
            }

            // Check AVIF signature
            if (header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70 &&
                header[8] == 0x61 && header[9] == 0x76 && header[10] == 0x69 && (header[11] == 0x66 || header[11] == 0x73))
            {
                return new ImageInspectionResult(false, null, null, 0, 0, 0, false,
                    "Le format AVIF/AVIS n'est pas supporté. Formats acceptés : JPEG, PNG et WebP uniquement.");
            }

            string extension;
            string contentType;

            ReadOnlySpan<byte> span = header;
            if (span[0] == 0xFF && span[1] == 0xD8 && span[2] == 0xFF)
            {
                extension = ".jpg";
                contentType = "image/jpeg";
            }
            else if (span[0] == 0x89 && span[1] == 0x50 && span[2] == 0x4E && span[3] == 0x47 &&
                     span[4] == 0x0D && span[5] == 0x0A && span[6] == 0x1A && span[7] == 0x0A)
            {
                extension = ".png";
                contentType = "image/png";
            }
            else if (span[0] == 0x52 && span[1] == 0x49 && span[2] == 0x46 && span[3] == 0x46 &&
                     span[8] == 0x57 && span[9] == 0x45 && span[10] == 0x42 && span[11] == 0x50)
            {
                extension = ".webp";
                contentType = "image/webp";
            }
            else
            {
                return new ImageInspectionResult(false, null, null, 0, 0, 0, false,
                    "Format d'image non supporté. Formats acceptés : JPEG, PNG et WebP uniquement.");
            }

            // Check animation
            if (stream.CanSeek)
            {
                stream.Position = 0;
                if (extension == ".png" && CheckPngAnimation(stream))
                {
                    return new ImageInspectionResult(false, extension, contentType, 0, 0, 0, true,
                        "Les images PNG animées (APNG) ne sont pas supportées.");
                }

                if (extension == ".webp" && CheckWebpAnimation(stream))
                {
                    return new ImageInspectionResult(false, extension, contentType, 0, 0, 0, true,
                        "Les images WebP animées ne sont pas supportées.");
                }

                stream.Position = 0;
            }

            // Decode bounds with SKCodec without disposing the underlying stream
            using var managedStream = new SKManagedStream(stream, false);
            using var codec = SKCodec.Create(managedStream);
            if (codec == null)
            {
                return new ImageInspectionResult(false, extension, contentType, 0, 0, 0, false,
                    "Le fichier image est corrompu ou illisible.");
            }

            var width = codec.Info.Width;
            var height = codec.Info.Height;

            if (width <= 0 || height <= 0)
            {
                return new ImageInspectionResult(false, extension, contentType, width, height, 0, false,
                    "Dimensions d'image invalides.");
            }

            long pixelCount;
            try
            {
                pixelCount = checked((long)width * height);
            }
            catch (OverflowException)
            {
                return new ImageInspectionResult(false, extension, contentType, width, height, long.MaxValue, false,
                    $"L'image dépasse la limite autorisée de {MaxPixelCount:N0} pixels.");
            }

            if (pixelCount > MaxPixelCount)
            {
                return new ImageInspectionResult(false, extension, contentType, width, height, pixelCount, false,
                    $"L'image dépasse la limite autorisée de {MaxPixelCount:N0} pixels ({pixelCount:N0} pixels détectés).");
            }

            return new ImageInspectionResult(true, extension, contentType, width, height, pixelCount, false, null);
        }
        catch (Exception ex)
        {
            return new ImageInspectionResult(false, null, null, 0, 0, 0, false,
                $"Erreur lors de l'inspection de l'image : {ex.Message}");
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    private static bool CheckPngAnimation(Stream stream)
    {
        try
        {
            // PNG signature is 8 bytes
            if (stream.Length < 8)
            {
                return false;
            }
            stream.Position = 8;

            var chunkHeader = new byte[8];
            // Limit scanning to avoid excessive reading
            while (stream.Position + 8 <= stream.Length)
            {
                var bytesRead = stream.Read(chunkHeader, 0, 8);
                if (bytesRead < 8)
                {
                    break;
                }

                var length = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader.AsSpan(0, 4));
                var chunkType = Encoding.ASCII.GetString(chunkHeader, 4, 4);

                if (chunkType == "acTL")
                {
                    return true;
                }

                // In APNG, acTL must appear before the first IDAT
                if (chunkType == "IDAT" || chunkType == "IEND")
                {
                    break;
                }

                // Next chunk offset = length + 4 (CRC)
                var nextPos = stream.Position + length + 4;
                if (nextPos < stream.Position || nextPos > stream.Length)
                {
                    break; // Corrupted length
                }
                stream.Position = nextPos;
            }
        }
        catch
        {
            // Ignore parsing errors and assume non-animated
        }

        return false;
    }

    private static bool CheckWebpAnimation(Stream stream)
    {
        try
        {
            // WebP header: 4 (RIFF) + 4 (size) + 4 (WEBP) = 12 bytes
            if (stream.Length < 12)
            {
                return false;
            }
            stream.Position = 12;

            var chunkHeader = new byte[8];
            while (stream.Position + 8 <= stream.Length)
            {
                var bytesRead = stream.Read(chunkHeader, 0, 8);
                if (bytesRead < 8)
                {
                    break;
                }

                var fourCc = Encoding.ASCII.GetString(chunkHeader, 0, 4);
                var length = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(4, 4));

                if (fourCc == "ANIM" || fourCc == "ANMF")
                {
                    return true;
                }

                if (fourCc == "VP8X" && length >= 10 && stream.Position + 1 <= stream.Length)
                {
                    var flagByte = stream.ReadByte();
                    if (flagByte != -1 && (flagByte & 0x02) != 0) // Bit 1 is Animation flag
                    {
                        return true;
                    }
                    // Move remaining of VP8X chunk: length - 1 + padding
                    var remaining = (length - 1) + (length & 1);
                    stream.Position += remaining;
                    continue;
                }

                // Advance chunk data + 1 byte padding if length is odd
                var padding = (length & 1);
                var nextPos = stream.Position + length + padding;
                if (nextPos < stream.Position || nextPos > stream.Length)
                {
                    break;
                }
                stream.Position = nextPos;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }
}


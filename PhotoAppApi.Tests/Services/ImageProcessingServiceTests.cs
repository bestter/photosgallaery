using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoAppApi.Services;
using SkiaSharp;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class ImageProcessingServiceTests
    {
        private readonly ImageProcessingService _service = new();

        private static byte[] CreateAsymmetricImage(int width, int height, SKEncodedImageFormat format, SKEncodedOrigin origin = SKEncodedOrigin.TopLeft)
        {
            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            // Fill background with red
            canvas.Clear(new SKColor(255, 0, 0, 255));

            // Draw green top-left corner marker (30x10) to make it distinct and asymmetric
            using var paintGreen = new SKPaint { Color = new SKColor(0, 255, 0, 255) };
            canvas.DrawRect(0, 0, Math.Min(30, width / 2), Math.Min(10, height / 2), paintGreen);

            // Draw blue bottom-right marker
            using var paintBlue = new SKPaint { Color = new SKColor(0, 0, 255, 255) };
            canvas.DrawRect(width - 10, height - 10, 10, 10, paintBlue);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(format, 90);
            return data.ToArray();
        }

        private static byte[] CreateApngBytes()
        {
            // Valid PNG with acTL chunk before IDAT
            using var ms = new MemoryStream();
            // PNG header
            ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

            // IHDR chunk: 13 bytes
            byte[] ihdrData = new byte[] {
                0x00, 0x00, 0x00, 0x01, // width 1
                0x00, 0x00, 0x00, 0x01, // height 1
                0x08, 0x06, 0x00, 0x00, 0x00
            };
            WritePngChunk(ms, "IHDR", ihdrData);

            // acTL chunk (APNG indicator)
            byte[] actlData = new byte[] {
                0x00, 0x00, 0x00, 0x02, // num frames: 2
                0x00, 0x00, 0x00, 0x00  // num plays: 0 (infinite)
            };
            WritePngChunk(ms, "acTL", actlData);

            // IDAT chunk
            byte[] idatData = new byte[] { 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01 };
            WritePngChunk(ms, "IDAT", idatData);

            // IEND chunk
            WritePngChunk(ms, "IEND", Array.Empty<byte>());

            return ms.ToArray();
        }

        private static void WritePngChunk(Stream stream, string type, byte[] data)
        {
            byte[] lenBytes = BitConverter.GetBytes((uint)data.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            stream.Write(lenBytes);

            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes);
            stream.Write(data);

            // Dummy CRC
            stream.Write(new byte[] { 0, 0, 0, 0 });
        }

        private static byte[] CreateAnimatedWebpBytes()
        {
            // WebP with VP8X and animation bit (0x02) set
            using var ms = new MemoryStream();
            ms.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            ms.Write(BitConverter.GetBytes((uint)30)); // File size
            ms.Write(System.Text.Encoding.ASCII.GetBytes("WEBP"));

            // VP8X chunk
            ms.Write(System.Text.Encoding.ASCII.GetBytes("VP8X"));
            ms.Write(BitConverter.GetBytes((uint)10)); // Chunk length
            // Byte 0 of payload: flags with bit 1 (0x02) set
            ms.Write(new byte[] { 0x02, 0x00, 0x00, 0x00 });
            // Canvas width (3 bytes) and height (3 bytes)
            ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            return ms.ToArray();
        }

        [Fact]
        public void Inspect_Apng_RejectedAsAnimated()
        {
            byte[] apng = CreateApngBytes();
            using var stream = new MemoryStream(apng);

            var result = _service.Inspect(stream);

            Assert.False(result.IsValid);
            Assert.True(result.IsAnimated);
            Assert.Contains("APNG", result.ErrorMessage);
        }

        [Fact]
        public void Inspect_AnimatedWebp_RejectedAsAnimated()
        {
            byte[] webp = CreateAnimatedWebpBytes();
            using var stream = new MemoryStream(webp);

            var result = _service.Inspect(stream);

            Assert.False(result.IsValid);
            Assert.True(result.IsAnimated);
            Assert.Contains("WebP", result.ErrorMessage);
        }

        [Fact]
        public void Inspect_Avif_Rejected()
        {
            byte[] avifHeader = new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 };
            using var stream = new MemoryStream(avifHeader);

            var result = _service.Inspect(stream);

            Assert.False(result.IsValid);
            Assert.Contains("AVIF", result.ErrorMessage);
        }

        [Fact]
        public void Inspect_TruncatedData_Rejected()
        {
            byte[] truncated = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
            using var stream = new MemoryStream(truncated);

            var result = _service.Inspect(stream);

            Assert.False(result.IsValid);
            Assert.Contains("corrompu", result.ErrorMessage);
        }

        private static byte[] CreateAsymmetricImageWithExif(int width, int height, ushort orientation)
        {
            byte[] baseJpeg = CreateAsymmetricImage(width, height, SKEncodedImageFormat.Jpeg);
            using var app1Ms = new MemoryStream();
            app1Ms.Write(new byte[] { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 });
            app1Ms.Write(new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 });
            app1Ms.Write(BitConverter.GetBytes((ushort)1));
            app1Ms.Write(BitConverter.GetBytes((ushort)0x0112));
            app1Ms.Write(BitConverter.GetBytes((ushort)3));
            app1Ms.Write(BitConverter.GetBytes((uint)1));
            app1Ms.Write(BitConverter.GetBytes(orientation));
            app1Ms.Write(new byte[] { 0x00, 0x00 });
            app1Ms.Write(BitConverter.GetBytes((uint)0));

            byte[] exifPayload = app1Ms.ToArray();
            ushort app1Len = (ushort)(exifPayload.Length + 2);

            using var outMs = new MemoryStream();
            outMs.Write(new byte[] { 0xFF, 0xD8 });
            outMs.Write(new byte[] { 0xFF, 0xE1 });
            byte[] lenBytes = BitConverter.GetBytes(app1Len);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            outMs.Write(lenBytes);
            outMs.Write(exifPayload);
            outMs.Write(baseJpeg, 2, baseJpeg.Length - 2);

            return outMs.ToArray();
        }

        [Theory]
        [InlineData((ushort)1, 100, 50, 100, 50)]
        [InlineData((ushort)2, 100, 50, 100, 50)]
        [InlineData((ushort)3, 100, 50, 100, 50)]
        [InlineData((ushort)4, 100, 50, 100, 50)]
        [InlineData((ushort)5, 100, 50, 50, 100)]
        [InlineData((ushort)6, 100, 50, 50, 100)]
        [InlineData((ushort)7, 100, 50, 50, 100)]
        [InlineData((ushort)8, 100, 50, 50, 100)]
        public async Task ProcessImageAsync_AllEightOrientations_NormalizesDimensions(
            ushort orientation, int rawW, int rawH, int expectedNormW, int expectedNormH)
        {
            byte[] bytes = CreateAsymmetricImageWithExif(rawW, rawH, orientation);
            using var stream = new MemoryStream(bytes);

            using var result = await _service.ProcessImageAsync(stream, includeGps: false, CancellationToken.None);

            Assert.Equal(expectedNormW, result.NormalizedWidth);
            Assert.Equal(expectedNormH, result.NormalizedHeight);
            Assert.True(result.MainStream.Length > 0);
            Assert.True(result.ThumbnailStream.Length > 0);
        }

        [Fact]
        public async Task ProcessImageAsync_MetadataStripped_OutputsHaveNoExifIptcXmp()
        {
            byte[] bytes = CreateAsymmetricImage(80, 60, SKEncodedImageFormat.Jpeg);
            using var stream = new MemoryStream(bytes);

            using var result = await _service.ProcessImageAsync(stream, includeGps: false, CancellationToken.None);

            // Read metadata from MainStream
            result.MainStream.Position = 0;
            var mainDirectories = ImageMetadataReader.ReadMetadata(result.MainStream);
            Assert.DoesNotContain(mainDirectories, d => d is ExifSubIfdDirectory || d is GpsDirectory);

            // Read metadata from ThumbnailStream
            result.ThumbnailStream.Position = 0;
            var thumbDirectories = ImageMetadataReader.ReadMetadata(result.ThumbnailStream);
            Assert.DoesNotContain(thumbDirectories, d => d is ExifSubIfdDirectory || d is GpsDirectory);
        }

        [Fact]
        public async Task ProcessImageAsync_ThumbnailDimensions_RespectsBoundaryAndNoUpscale()
        {
            // Image smaller than 400x400 must NOT be upscaled
            byte[] smallBytes = CreateAsymmetricImage(250, 150, SKEncodedImageFormat.Png);
            using var smallStream = new MemoryStream(smallBytes);

            using var smallResult = await _service.ProcessImageAsync(smallStream, includeGps: false, CancellationToken.None);

            Assert.Equal(250, smallResult.ThumbnailWidth);
            Assert.Equal(150, smallResult.ThumbnailHeight);

            // Image larger than 400x400 must be scaled down to max 400x400
            byte[] largeBytes = CreateAsymmetricImage(1200, 600, SKEncodedImageFormat.Jpeg);
            using var largeStream = new MemoryStream(largeBytes);

            using var largeResult = await _service.ProcessImageAsync(largeStream, includeGps: false, CancellationToken.None);

            Assert.Equal(400, largeResult.ThumbnailWidth);
            Assert.Equal(200, largeResult.ThumbnailHeight);
        }

        [Fact]
        public async Task ProcessImageAsync_PreservesTransparencyInPng()
        {
            // Create 50x50 transparent PNG with a green box
            var info = new SKImageInfo(50, 50, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { Color = SKColors.Green };
            canvas.DrawRect(10, 10, 30, 30, paint);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());

            using var result = await _service.ProcessImageAsync(stream, includeGps: false, CancellationToken.None);

            // Decode the result to verify corner is still transparent
            using var outputBitmap = SKBitmap.Decode(result.MainStream);
            var cornerPixel = outputBitmap.GetPixel(0, 0);
            Assert.Equal(0, cornerPixel.Alpha);
        }
    }
}

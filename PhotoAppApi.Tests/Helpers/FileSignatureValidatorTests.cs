using PhotoAppApi.Helpers;
using System.IO;
using Xunit;

namespace PhotoAppApi.Tests.Helpers
{
    public class FileSignatureValidatorTests
    {
        [Fact]
        public void IsValidImage_NullStream_ReturnsFalse()
        {
            // Arrange
            Stream stream = null;

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.False(result);
            Assert.Equal(string.Empty, extension);
        }

        [Fact]
        public void IsValidImage_StreamTooShort_ReturnsFalse()
        {
            // Arrange
            var bytes = new byte[5] { 0xFF, 0xD8, 0xFF, 0, 0 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.False(result);
            Assert.Equal(string.Empty, extension);
        }

        [Fact]
        public void IsValidImage_ValidJpg_ReturnsTrue()
        {
            // Arrange
            var bytes = new byte[12] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.True(result);
            Assert.Equal(".jpg", extension);
        }

        [Fact]
        public void IsValidImage_ValidPng_ReturnsTrue()
        {
            // Arrange
            var bytes = new byte[12] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.True(result);
            Assert.Equal(".png", extension);
        }

        [Fact]
        public void IsValidImage_ValidWebp_ReturnsTrue()
        {
            // Arrange
            var bytes = new byte[12] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.True(result);
            Assert.Equal(".webp", extension);
        }

        [Fact]
        public void IsValidImage_ValidAvif_ReturnsTrue()
        {
            // Arrange
            var bytes = new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.True(result);
            Assert.Equal(".avif", extension);
        }

        [Fact]
        public void IsValidImage_ValidAvis_ReturnsTrue()
        {
            // Arrange
            var bytes = new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x73 };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.True(result);
            Assert.Equal(".avif", extension);
        }

        [Fact]
        public void IsValidImage_InvalidImage_ReturnsFalse()
        {
            // Arrange
            var bytes = new byte[12] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C };
            using var stream = new MemoryStream(bytes);

            // Act
            bool result = FileSignatureValidator.IsValidImage(stream, out string extension);

            // Assert
            Assert.False(result);
            Assert.Equal(string.Empty, extension);
        }

        [Fact]
        public void IsValidImage_ResetsStreamPosition()
        {
            // Arrange
            var bytes = new byte[12] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            using var stream = new MemoryStream(bytes);
            stream.Position = 5; // Should be reset to 0 internally, and end up at 0

            // Act
            FileSignatureValidator.IsValidImage(stream, out _);

            // Assert
            Assert.Equal(0, stream.Position);
        }
    }
}

using PhotoAppApi.Services;
using Xunit;

namespace PhotoAppApi.Tests.Services
{
    public class SlugGeneratorTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void GenerateSlug_NullOrWhiteSpace_ReturnsEmptyString(string? input, string expected)
        {
            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_RemovesDiacritics()
        {
            // Arrange
            string input = "crème brûlée";
            string expected = "creme-brulee";

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_ConvertsToLowerCase()
        {
            // Arrange
            string input = "Hello World";
            string expected = "hello-world";

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_RemovesNonAlphanumeric()
        {
            // Arrange
            string input = "Hello, World! @2024";
            string expected = "hello-world-2024";

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_ConvertsMultipleSpacesToOneDash()
        {
            // Arrange
            string input = "Hello   World";
            string expected = "hello-world";

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_TrimsLeadingAndTrailingDashes()
        {
            // Arrange
            string input = " -Hello World- ";
            string expected = "hello-world";

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSlug_LimitsLengthTo50()
        {
            // Arrange
            string input = "This is a very long string that should definitely exceed the fifty character limit for slugs";
            // "this-is-a-very-long-string-that-should-definitely-exceed" -> length is 56.
            // Substring 50: "this-is-a-very-long-string-that-should-definitely-"
            // Trim '-': "this-is-a-very-long-string-that-should-definitely"

            // Act
            string result = SlugGenerator.GenerateSlug(input);

            // Assert
            Assert.True(result.Length <= 50, $"Length is {result.Length}");
            Assert.Equal("this-is-a-very-long-string-that-should-definitely", result);
        }
    }
}

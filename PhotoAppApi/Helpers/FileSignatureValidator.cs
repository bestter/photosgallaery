using System;
using System.IO;

namespace PhotoAppApi.Helpers
{
    public static class FileSignatureValidator
    {
        public static bool IsValidImage(Stream stream, out string extension)
        {
            extension = string.Empty;
            if (stream == null || stream.Length < 12)
            {
                return false;
            }

            var headerBytes = new byte[12];
            stream.Position = 0;

            int bytesRead = stream.Read(headerBytes, 0, 12);
            stream.Position = 0; // Reset stream position

            if (bytesRead < 12)
            {
                return false;
            }

            extension = ((ReadOnlySpan<byte>)headerBytes) switch
            {
                [0xFF, 0xD8, 0xFF, ..] => ".jpg",
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => ".png",
                [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => ".webp",
                [_, _, _, _, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66 or 0x73, ..] => ".avif",
                _ => string.Empty
            };

            return extension != string.Empty;
        }
    }
}

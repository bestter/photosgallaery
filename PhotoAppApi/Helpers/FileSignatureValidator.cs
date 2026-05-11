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

            if (headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            {
                extension = ".jpg";
                return true;
            }

            if (headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47 &&
                headerBytes[4] == 0x0D && headerBytes[5] == 0x0A && headerBytes[6] == 0x1A && headerBytes[7] == 0x0A)
            {
                extension = ".png";
                return true;
            }

            if (headerBytes[0] == 0x52 && headerBytes[1] == 0x49 && headerBytes[2] == 0x46 && headerBytes[3] == 0x46 &&
                headerBytes[8] == 0x57 && headerBytes[9] == 0x45 && headerBytes[10] == 0x42 && headerBytes[11] == 0x50) // RIFF...WEBP
            {
                extension = ".webp";
                return true;
            }

            // AVIF check
            // Usually starts with size (4 bytes), 'ftyp' (4 bytes), then 'avif' or 'avis' (4 bytes)
            if (headerBytes[4] == 0x66 && headerBytes[5] == 0x74 && headerBytes[6] == 0x79 && headerBytes[7] == 0x70) // ftyp
            {
                if (headerBytes[8] == 0x61 && headerBytes[9] == 0x76 && headerBytes[10] == 0x69 && (headerBytes[11] == 0x66 || headerBytes[11] == 0x73)) // avif or avis
                {
                    extension = ".avif";
                    return true;
                }
            }

            return false;
        }
    }
}

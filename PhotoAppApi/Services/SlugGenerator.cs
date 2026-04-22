using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoAppApi.Services
{
    public static class SlugGenerator
    {
        public static string GenerateSlug(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return string.Empty;

            // 1. Enlever les accents (diacritiques)
            string str = RemoveDiacritics(phrase);

            // 2. Mettre en minuscules
            str = str.ToLowerInvariant();

            // 3. Remplacer les caractères non-alphanumériques par des espaces
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

            // 4. Convertir les espaces (et espaces multiples) en un seul tiret
            str = Regex.Replace(str, @"\s+", "-").Trim('-');

            // 5. Optionnel : limiter la longueur (ex: 50 caractères max)
            if (str.Length > 50)
                str = str.Substring(0, 50).Trim('-');

            return str;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
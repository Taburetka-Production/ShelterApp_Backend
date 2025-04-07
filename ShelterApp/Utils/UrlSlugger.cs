using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;

namespace ShelterApp
{
    public static class UrlSlugger
    {
        private static readonly Dictionary<char, string> UkrainianToLatinMap = new Dictionary<char, string>
        {
            {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "h"}, {'ґ', "g"},
            {'д', "d"}, {'е', "e"}, {'є', "ye"}, {'ж', "zh"}, {'з', "z"},
            {'и', "y"}, {'і', "i"}, {'ї', "yi"}, {'й', "y"}, {'к', "k"},
            {'л', "l"}, {'м', "m"}, {'н', "n"}, {'о', "o"}, {'п', "p"},
            {'р', "r"}, {'с', "s"}, {'т', "t"}, {'у', "u"}, {'ф', "f"},
            {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"}, {'ш', "sh"}, {'щ', "shch"},
            {'ь', ""}, {'ю', "yu"}, {'я', "ya"}
        };


        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var slug = input.ToLowerInvariant();

            slug = TransliterateUkrainianToLatin(slug);

            slug = RemoveDiacritics(slug);

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "", RegexOptions.Compiled);

            slug = Regex.Replace(slug, @"\s+", "-", RegexOptions.Compiled).Trim('-');

            slug = Regex.Replace(slug, @"-+", "-", RegexOptions.Compiled);
            
            return slug;
        }

        private static string TransliterateUkrainianToLatin(string text)
        {
            var stringBuilder = new StringBuilder(text.Length);

            foreach (char c in text)
            {
                if (UkrainianToLatinMap.TryGetValue(c, out string? latinEquivalent))
                {
                    stringBuilder.Append(latinEquivalent);
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }


        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }
    }
}
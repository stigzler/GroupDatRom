using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace GroupDatRom.Mappers
{
    internal static class MediaTypeMapper
    {
        private static readonly string ConfigFileName = "MediaTypeMappings.json";
        // Removed bare "d" to prevent matching arbitrary letters inside words like EasyFlash/Deutsch
        private static List<string> _discTags = new() { "disc", "disk", "cd" };
        private static List<string> _sideTags = new() { "side" };
        private static List<string> _otherTags = new() { "tape", "cart", "part", "vol", "volume" };

        public static void Initialize()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

            if (!File.Exists(filePath))
            {
                var defaultContent = new Dictionary<string, List<string>>
                {
                    { "DiscTags", _discTags },
                    { "SideTags", _sideTags },
                    { "TapeCartTags", _otherTags }
                };
                File.WriteAllText(filePath, JsonSerializer.Serialize(defaultContent, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                LoadConfig(filePath);
            }
        }

        private static void LoadConfig(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString);
                if (dict != null)
                {
                    if (dict.TryGetValue("DiscTags", out var discs)) _discTags = discs;
                    if (dict.TryGetValue("SideTags", out var sides)) _sideTags = sides;
                    if (dict.TryGetValue("TapeCartTags", out var others)) _otherTags = others;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {ConfigFileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a filename contains a media type keyword inside brackets/parentheses 
        /// (e.g., "(Disc 1)", "[Disk A]", "(Side 1A)") and extracts the clean base group name.
        /// </summary>
        public static bool TryGetMultiMediaGroupName(string fileNameWithoutExtension, out string groupName)
        {
            groupName = fileNameWithoutExtension;

            var allTags = new List<string>();
            allTags.AddRange(_discTags);
            allTags.AddRange(_sideTags);
            allTags.AddRange(_otherTags);

            // Use word boundaries \b so keywords like "disk" match whole words only, ignoring words like EasyFlash/Deutsch
            string tagKeywords = string.Join("|", allTags);
            string pattern = $@"\s*[\[\(][^\]\)]*\b(?:{tagKeywords})\b[^\(\)\[\]]*[\]\)]\s*";

            var match = Regex.Match(fileNameWithoutExtension, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Remove the media tag from wherever it sits in the filename
                string cleaned = fileNameWithoutExtension.Replace(match.Value, " ");
                cleaned = Regex.Replace(cleaned, @"\s{2,}", " "); // Collapse double spaces
                cleaned = cleaned.Trim();

                if (!string.IsNullOrEmpty(cleaned) && !cleaned.Equals(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                {
                    groupName = cleaned;
                    return true;
                }
            }

            return false;
        }
    }
}
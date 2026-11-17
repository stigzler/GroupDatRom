using GroupDatRom.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GroupDatRom.Mappers
{
    internal class FileTypeMapper
    {
        private static readonly string ConfigFileName = "FileTypeMappings.json";
        private static Dictionary<string, NodeType> _extensionCache = new(StringComparer.OrdinalIgnoreCase);

        // Default fallback JSON stored natively in code so it auto-creates if missing
        private const string DefaultJsonContent = @"{
"".m3u"": ""FileM3u"",
"".cue"": ""FileCue"",
"".gdi"": ""FileGdi"",
  
"".chd"": ""FileChd"",
"".rvz"": ""FileRvzWia"",
"".wia"": ""FileRvzWia"",
"".gcz"": ""FileRvzWia"",
"".cso"": ""FileCsoZso"",
"".zso"": ""FileCsoZso"",
"".dax"": ""FileCsoZso"",
"".3ds"": ""File3DsContainer"",
"".z3ds"": ""File3DsContainer"",
"".iso"": ""FileStandardIso"",
"".bin"": ""FileBin"",
"".wbfs"": ""FileStandardIso"",
"".wux"": ""FileStandardIso"",
  
"".nes"": ""FileCartridge"",
"".sfc"": ""FileCartridge"",
"".md"": ""FileCartridge"",
"".gb"": ""FileCartridge"",
"".gba"": ""FileCartridge"",
"".n64"": ""FileCartridge"",
  
"".atr"": ""FileVintageMedia"",
"".dsk"": ""FileVintageMedia"",
"".cas"": ""FileVintageMedia"",
"".d64"": ""FileVintageMedia"",
"".adf"": ""FileVintageMedia"",
"".st"": ""FileVintageMedia"",

"".png"": ""FileImage"",
"".jpg"": ""FileImage"",
"".jpeg"": ""FileImage"",
"".webp"": ""FileImage"",
"".bmp"": ""FileImage"",

"".mp3"": ""FileMusic"",
"".flac"": ""FileMusic"",
"".ogg"": ""FileMusic"",
"".wav"": ""FileMusic"",
"".aac"": ""FileMusic"",

"".zip"": ""Archive"",
"".7z"": ""Archive"",
"".rar"": ""Archive"",
"".tar"": ""Archive"",
"".gz"": ""Archive"",
"".bz2"": ""Archive""
}";

        /// <summary>
        /// Ensures the mapping file exists in the app root, then loads it into memory.
        /// Call this once when your application starts up.
        /// </summary>
        public static void Initialize()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

            // If the file doesn't exist in the root directory, write the default one out
            if (!File.Exists(filePath))
            {
                File.WriteAllText(filePath, DefaultJsonContent);
            }

            LoadMappings(filePath);
        }

        private static void LoadMappings(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var rawDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString, options);

                if (rawDictionary != null)
                {
                    _extensionCache.Clear();
                    foreach (var kvp in rawDictionary)
                    {
                        if (Enum.TryParse<NodeType>(kvp.Value, true, out var nodeType))
                        {
                            string ext = kvp.Key.StartsWith(".") ? kvp.Key : "." + kvp.Key;
                            _extensionCache[ext] = nodeType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                        $"Error reading {ConfigFileName}: {ex.Message}\nUsing default mappings.",
                        "Config Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Gets the NodeType for a given file extension (e.g., ".png"). Returns FileOther if unknown.
        /// </summary>
        public static NodeType GetNodeType(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return NodeType.FileOther;

            string normalizedExt = extension.StartsWith(".") ? extension : "." + extension;

            if (_extensionCache.TryGetValue(normalizedExt, out var nodeType))
            {
                return nodeType;
            }

            return NodeType.FileOther;
        }
    }
}

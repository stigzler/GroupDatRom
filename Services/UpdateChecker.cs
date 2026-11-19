using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupDatRom.Services
{
    public sealed record UpdateInfo(
    bool IsUpdateAvailable,
    Version CurrentVersion,
    Version LatestVersion,
    string ReleasePageUrl,
    string? ZipDownloadUrl,
    string? ReleaseNotes);

    internal static class UpdateChecker
    {
        private const string Owner = "stigzler";
        private const string Repo = "GroupDatRom";

        private static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Repo, "1.0"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        public static async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
                // Normalize 4-part assembly version (e.g. 0.9.11.0 -> 0.9.11) for clean comparison
                current = new Version(current.Major, Math.Max(0, current.Minor), Math.Max(0, current.Build));

                var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
                using var response = await Http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync();
                var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream);
                if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                    return null;

                var cleanTag = release.TagName.TrimStart('v', 'V');
                if (!Version.TryParse(cleanTag, out var latest))
                    return null;

                latest = new Version(latest.Major, Math.Max(0, latest.Minor), Math.Max(0, latest.Build));

                var zipAsset = release.Assets?.FirstOrDefault(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                return new UpdateInfo(
                    IsUpdateAvailable: latest > current,
                    CurrentVersion: current,
                    LatestVersion: latest,
                    ReleasePageUrl: release.HtmlUrl,
                    ZipDownloadUrl: zipAsset?.BrowserDownloadUrl,
                    ReleaseNotes: release.Body);
            }
            catch
            {
                // Offline, timeout, or rate-limited — fail silently so startup isn't disrupted
                return null;
            }
        }

        public static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = string.Empty;

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = string.Empty;

            [JsonPropertyName("body")]
            public string? Body { get; set; }

            [JsonPropertyName("assets")]
            public GitHubAsset[]? Assets { get; set; }
        }

        private sealed class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = string.Empty;
        }
    }
}

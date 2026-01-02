using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AI.DiffAssistant.Shared.Models;

namespace AI.DiffAssistant.Core.Release;

/// <summary>
/// 版本发布服务
/// </summary>
public class ReleaseService
{
    private const string DefaultApiBaseUrl = "https://api.github.com";
    private const string PlatformLabel = "x64";
    private const string UserAgent = "diff-check";

    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public ReleaseService(HttpClient httpClient, string? apiBaseUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUri = new Uri(apiBaseUrl ?? DefaultApiBaseUrl);
    }

    public async Task<IReadOnlyList<ReleaseInfo>> GetStableReleasesAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("Owner is required.", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(repo))
        {
            throw new ArgumentException("Repo is required.", nameof(repo));
        }

        var requestUri = new Uri(_baseUri, $"/repos/{owner}/{repo}/releases");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(UserAgent, "1.0"));
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ReleaseInfo>();
        }

        var releases = new List<ReleaseInfo>();
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (IsPrereleaseOrDraft(release))
            {
                continue;
            }

            var version = GetString(release, "tag_name");
            if (string.IsNullOrWhiteSpace(version))
            {
                version = GetString(release, "name");
            }

            var publishedAt = GetDateTimeOffset(release, "published_at");
            var summary = ExtractSummary(GetString(release, "body"));
            var downloadUrl = GetDownloadUrl(release);

            releases.Add(new ReleaseInfo
            {
                Version = string.IsNullOrWhiteSpace(version) ? "unknown" : version,
                PublishedAt = publishedAt,
                Platform = PlatformLabel,
                Summary = summary,
                DownloadUrl = downloadUrl
            });
        }

        return releases
            .OrderByDescending(r => r.PublishedAt)
            .ToList();
    }

    private static bool IsPrereleaseOrDraft(JsonElement release)
    {
        return GetBoolean(release, "prerelease") || GetBoolean(release, "draft");
    }

    private static string GetDownloadUrl(JsonElement release)
    {
        if (release.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var url = GetString(asset, "browser_download_url");
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
        }

        return GetString(release, "html_url");
    }

    private static string ExtractSummary(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        using var reader = new StringReader(body);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
               && property.ValueKind == JsonValueKind.True;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTimeOffset GetDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateTimeOffset.TryParse(value, out var result) ? result : DateTimeOffset.MinValue;
    }
}

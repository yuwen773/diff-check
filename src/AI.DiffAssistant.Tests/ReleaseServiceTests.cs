using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AI.DiffAssistant.Core.Release;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace AI.DiffAssistant.Tests;

public class ReleaseServiceTests
{
    [Fact]
    public async Task GetStableReleasesAsync_ShouldFilterAndSort()
    {
        using var server = WireMockServer.Start();
        var json = """
            [
              {
                "tag_name": "v1.0.0",
                "prerelease": false,
                "draft": false,
                "published_at": "2026-01-01T00:00:00Z",
                "body": "Summary 1",
                "assets": [
                  { "browser_download_url": "https://example.com/a.exe" }
                ],
                "html_url": "https://example.com/r1"
              },
              {
                "tag_name": "v2.0.0",
                "prerelease": false,
                "draft": false,
                "published_at": "2026-02-01T00:00:00Z",
                "body": "Summary 2",
                "assets": [
                  { "browser_download_url": "https://example.com/b.exe" }
                ],
                "html_url": "https://example.com/r2"
              },
              {
                "tag_name": "v2.1.0",
                "prerelease": true,
                "draft": false,
                "published_at": "2026-03-01T00:00:00Z",
                "body": "Beta",
                "assets": [],
                "html_url": "https://example.com/r3"
              },
              {
                "tag_name": "v0.9.0",
                "prerelease": false,
                "draft": true,
                "published_at": "2025-12-01T00:00:00Z",
                "body": "Draft",
                "assets": [],
                "html_url": "https://example.com/r4"
              }
            ]
            """;

        server.Given(Request.Create().WithPath("/repos/yuwen773/diff-check/releases").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(json));

        using var httpClient = new HttpClient();
        var service = new ReleaseService(httpClient, server.Urls[0]);

        var releases = await service.GetStableReleasesAsync("yuwen773", "diff-check");

        Assert.Equal(2, releases.Count);
        Assert.Equal("v2.0.0", releases[0].Version);
        Assert.Equal("v1.0.0", releases[1].Version);
        Assert.All(releases, r => Assert.Equal("x64", r.Platform));
    }

    [Fact]
    public async Task GetStableReleasesAsync_ShouldUseSummaryAndFallbackUrl()
    {
        using var server = WireMockServer.Start();
        var json = """
            [
              {
                "tag_name": "v1.0.0",
                "prerelease": false,
                "draft": false,
                "published_at": "2026-01-01T00:00:00Z",
                "body": "\n\nFirst line summary\nMore details",
                "assets": [],
                "html_url": "https://example.com/r1"
              }
            ]
            """;

        server.Given(Request.Create().WithPath("/repos/yuwen773/diff-check/releases").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(json));

        using var httpClient = new HttpClient();
        var service = new ReleaseService(httpClient, server.Urls[0]);

        var releases = await service.GetStableReleasesAsync("yuwen773", "diff-check");
        var release = releases.Single();

        Assert.Equal("First line summary", release.Summary);
        Assert.Equal("https://example.com/r1", release.DownloadUrl);
    }
}

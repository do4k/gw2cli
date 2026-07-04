using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GW2CLI.Services;

public class CachingHandler : DelegatingHandler
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".gw2cli", "cache");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method != HttpMethod.Get)
            return await base.SendAsync(request, ct);

        var cachePath = CachePath(request.RequestUri!.ToString());

        if (File.Exists(cachePath) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < Ttl)
        {
            var body = await File.ReadAllTextAsync(cachePath, ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        var response = await base.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Directory.CreateDirectory(CacheDir);
            await File.WriteAllTextAsync(cachePath, body, ct);
            response.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return response;
    }

    private static string CachePath(string url)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url))).ToLowerInvariant();
        return Path.Combine(CacheDir, hash + ".json");
    }
}

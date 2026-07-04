using System.Net.Http.Headers;

namespace GW2CLI.Services;

/// Injects the GW2 API key as a Bearer token on every outbound request.
/// Reads from ApiKeyContext (per-invocation override) falling back to ConfigService (stored key).
public class ApiKeyHandler(ConfigService config, ApiKeyContext keyContext) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var key = keyContext.Override ?? config.ApiKey;
        if (key is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return base.SendAsync(request, ct);
    }
}

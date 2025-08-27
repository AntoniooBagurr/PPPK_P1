using System.Net.Http.Headers;
using OncoWeb.Models;

namespace OncoWeb.Services;

public class IngestService
{
    private readonly IHttpClientFactory _http;
    private readonly IStorageService _storage;

    public IngestService(IHttpClientFactory http, IStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    public async Task<IngestResult> RunAsync(IngestRequest req, CancellationToken ct)
    {
        var result = new IngestResult();
        var bucket = $"tcga-{req.Cohort.ToLowerInvariant()}";

        await _storage.EnsureBucketAsync(bucket, ct);

        var client = _http.CreateClient();

        foreach (var raw in req.TsvUrls ?? Enumerable.Empty<string>())
        {
            var url = (raw ?? string.Empty).Trim();

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    throw new UriFormatException($"Neispravan URL: {url}");

                using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();

                var fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = $"file_{Guid.NewGuid():N}.tsv";

                var key = $"{req.Cohort}/{DateTime.UtcNow:yyyyMMdd_HHmmss}/{fileName}";

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var len = resp.Content.Headers.ContentLength ?? -1;
                var ctype = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                await _storage.PutAsync(bucket, key, stream, len, ctype, ct);

                result.Uploaded.Add($"{bucket}/{key}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{url} -> {ex.Message}");
            }
        }

        return result;
    }
}

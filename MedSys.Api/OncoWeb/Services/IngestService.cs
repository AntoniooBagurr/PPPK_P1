using System.Net.Http.Headers;
using OncoWeb.Models;

namespace OncoWeb.Services;

public class IngestService
{
    private readonly IHttpClientFactory _http;
    private readonly IStorageService _storage;
    private readonly Microsoft.Extensions.Options.IOptions<AppOptions> _opts;

    public IngestService(IHttpClientFactory http, IStorageService storage, Microsoft.Extensions.Options.IOptions<AppOptions> opts)
    {
        _http = http;
        _storage = storage;
        _opts = opts;
    }

    private static bool LooksLikeUrl(string s) => s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                                 s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // pomoć: generiraj moguće mirror URL-ove ako je stiglo samo ime datoteke
    private static IEnumerable<string> BuildCandidates(string fileName)
    {
        // redoslijed probavanja (neki znaju vraćati 403/404 – zato fallback)
        yield return $"https://tcga.xenahubs.net/download?filename={Uri.EscapeDataString(fileName)}";
        yield return $"https://gdc-hub.s3.us-east-1.amazonaws.com/download/{fileName}";
        yield return $"https://toil-xena-hub.s3.us-east-1.amazonaws.com/download/{fileName}";
    }

    public async Task<IngestResult> RunAsync(IngestRequest req, CancellationToken ct)
    {
        var result = new IngestResult();
        if (req?.Jobs == null || req.Jobs.Count == 0) return result;

        var bucket = _opts.Value.Minio.Bucket;
        await _storage.EnsureBucketAsync(bucket, ct);

        var client = _http.CreateClient();
        // jednostavan UA (bez “comment” dijela – izbjegava FormatException)
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OncoWeb", "1.0"));

        foreach (var job in req.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Cohort) || string.IsNullOrWhiteSpace(job.Url))
            {
                result.Errors.Add("Prazan cohort ili url.");
                continue;
            }

            var urlCandidates = new List<string>();
            if (LooksLikeUrl(job.Url))
            {
                urlCandidates.Add(job.Url.Trim());
            }
            else
            {
                urlCandidates.AddRange(BuildCandidates(job.Url.Trim()));
            }

            HttpResponseMessage? okResp = null;
            string? okUrl = null;

            foreach (var u in urlCandidates)
            {
                try
                {
                    var resp = await client.GetAsync(u, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        okResp = resp;
                        okUrl = u;
                        break;
                    }
                    resp.Dispose();
                }
                catch { /* ignoriraj i probaj sljedeći mirror */ }
            }

            if (okResp == null)
            {
                result.Errors.Add($"Nisam uspio dohvatiti {job.Url} s nijednog mirrora.");
                continue;
            }

            using (okResp)
            {
                var ctType = okResp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = Path.GetFileName(okResp.RequestMessage!.RequestUri!.LocalPath);
                if (string.IsNullOrWhiteSpace(fileName) || !fileName.Contains('.'))
                    fileName = Path.GetFileName(job.Url); // fallback

                var objectName = string.IsNullOrWhiteSpace(job.ObjectName)
                    ? $"{job.Cohort.ToLowerInvariant()}/{fileName}"
                    : job.Cohort.ToLowerInvariant() + "/" + job.ObjectName.TrimStart('/');

                await using var s = await okResp.Content.ReadAsStreamAsync(ct);
                await _storage.PutObjectAsync(bucket, objectName, s, ctType, ct);

                result.Downloaded.Add(new IngestItemResult
                {
                    Cohort = job.Cohort,
                    Url = okUrl!,
                    ObjectName = objectName,
                    Bytes = okResp.Content.Headers.ContentLength ?? 0
                });
            }
        }

        return result;
    }
}

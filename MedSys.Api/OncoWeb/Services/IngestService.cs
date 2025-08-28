using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using OncoWeb.Models;

namespace OncoWeb.Services;

public class IngestService
{
    private readonly IHttpClientFactory _http;
    private readonly IStorageService _storage;
    private readonly AppOptions _opts;

    public IngestService(IHttpClientFactory http, IStorageService storage, IOptions<AppOptions> opts)
    {
        _http = http;
        _storage = storage;
        _opts = opts.Value;
    }

    private static IEnumerable<string> BuildCandidatesFromUrlOrName(string input)
    {
        string? file = null;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            yield return input;

            var local = Path.GetFileName(uri.LocalPath);
            if (string.Equals(local, "download", StringComparison.OrdinalIgnoreCase))
            {
                var q = QueryHelpers.ParseQuery(uri.Query);
                if (q.TryGetValue("filename", out var fn) && !string.IsNullOrWhiteSpace(fn))
                    file = fn.ToString();
            }
            else file = local;
        }
        else file = input;

        if (!string.IsNullOrWhiteSpace(file))
        {
            var enc = Uri.EscapeDataString(file);
            yield return $"https://tcga.xenahubs.net/download?filename={enc}";
            yield return $"https://pancanatlas.xenahubs.net/download/{enc}";
            yield return $"https://pancanatlas.xenahubs.net/download?filename={enc}";
            yield return $"https://toil-xenahub.s3.us-east-1.amazonaws.com/download/{file}";
        }
    }

    public async Task<IngestResult> RunAsync(IngestRequest req, CancellationToken ct)
    {
        var result = new IngestResult();
        if (req?.Jobs == null || req.Jobs.Count == 0) return result;

        var bucket = _opts.Minio.Bucket;
        await _storage.EnsureBucketAsync(bucket, ct);

        var client = _http.CreateClient("xena");
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OncoWeb", "1.0"));

        foreach (var job in req.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Cohort) || string.IsNullOrWhiteSpace(job.Url))
            {
                result.Errors.Add("Prazan cohort ili url.");
                continue;
            }

            var candidates = BuildCandidatesFromUrlOrName(job.Url.Trim()).ToList();

            HttpResponseMessage? okResp = null;
            string? okUrl = null;

            foreach (var u in candidates)
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
                catch
                {
                    // probaj sljedeći mirror
                }
            }

            if (okResp == null)
            {
                result.Errors.Add($"Nisam uspio dohvatiti {job.Url} s nijednog mirrora.");
                continue;
            }

            using (okResp)
            {
                var contentType = okResp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                var reqUri = okResp.RequestMessage!.RequestUri!;
                var fileName = Path.GetFileName(reqUri.LocalPath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.Equals("download", StringComparison.OrdinalIgnoreCase))
                {
                    var q = QueryHelpers.ParseQuery(reqUri.Query);
                    if (q.TryGetValue("filename", out var fn) && !string.IsNullOrWhiteSpace(fn))
                        fileName = fn.ToString();
                }
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = Path.GetFileName(job.Url);

                var objectName = string.IsNullOrWhiteSpace(job.ObjectName)
                    ? $"{job.Cohort.ToLowerInvariant()}/{fileName}"
                    : job.Cohort.ToLowerInvariant() + "/" + job.ObjectName.TrimStart('/');

                await using var stream = await okResp.Content.ReadAsStreamAsync(ct);
                await _storage.PutObjectAsync(bucket, objectName, stream, contentType, ct);

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

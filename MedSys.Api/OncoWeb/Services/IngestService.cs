using Microsoft.Extensions.Options;
using OncoWeb.Models;
using System.Net.Http.Headers;

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

    public async Task<IngestResult> RunAsync(IngestRequest req, CancellationToken ct)
    {
        var result = new IngestResult();
        if (req?.Jobs == null || req.Jobs.Count == 0) return result;

        // bucket u MinIO
        const string bucket = "tcga";
        await _storage.EnsureBucketAsync(bucket, ct);

        var client = _http.CreateClient();
        // ProductInfoHeaderValue ne voli zagrade – zato TryAddWithoutValidation:
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "curl/8.7");

        foreach (var job in req.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Cohort) || string.IsNullOrWhiteSpace(job.Url))
            {
                result.Errors.Add("cohort i url su obavezni.");
                continue;
            }

            var candidates = BuildCandidates(job.Url);

            Exception? last = null;
            foreach (var uri in candidates)
            {
                try
                {
                    // 1) kratki “probe” – neki serveri blokiraju HEAD pa radimo GET s Range: 0-0
                    using (var probeReq = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        probeReq.Headers.Range = new RangeHeaderValue(0, 0);
                        using var probe = await client.SendAsync(
                            probeReq,
                            HttpCompletionOption.ResponseHeadersRead, ct);

                        if (!probe.IsSuccessStatusCode)
                        {
                            last = new HttpRequestException($"{(int)probe.StatusCode} {probe.ReasonPhrase}");
                            continue;
                        }
                    }

                    // 2) puni download
                    using var resp = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                    resp.EnsureSuccessStatusCode();

                    var contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    var size = resp.Content.Headers.ContentLength ?? -1;
                    await using var stream = await resp.Content.ReadAsStreamAsync(ct);

                    var fileName = Path.GetFileName(uri.LocalPath);
                    var objectName = $"{job.Cohort.ToLowerInvariant()}/{fileName}";

                    await _storage.PutObjectAsync(bucket, objectName, stream, contentType, size, ct);

                    result.Downloaded.Add(new IngestItemResult
                    {
                        Cohort = job.Cohort,
                        Url = uri.ToString(),
                        ObjectName = objectName,
                        Bytes = size
                    });

                    last = null;
                    break; // ovaj kandidat je uspio
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            if (last != null)
                result.Errors.Add($"{job.Url}: {last.Message}");
        }

        if (result.Downloaded.Count == 0 && result.Errors.Count > 0)
            throw new HttpRequestException($"Nisam uspio dohvatiti {req.Jobs[0].Url} s nijednog mirrora.");

        return result;
    }

    public async Task UploadLocalAsync(string objectName, string contentType, Stream data, long size, CancellationToken ct)
    {
        const string bucket = "tcga";
        await _storage.EnsureBucketAsync(bucket, ct);
        await _storage.PutObjectAsync(bucket, objectName, data, contentType, size, ct);
    }

    private static IEnumerable<Uri> BuildCandidates(string given)
    {
        // ako dođe puni apsolutni URL i nije S3 – probaj njega
        if (Uri.TryCreate(given, UriKind.Absolute, out var abs))
        {
            // Ako je netko ipak zalijepio s3.amazonaws.com link, preskačemo ga (403 bez potpisa)
            if (!abs.Host.Contains("amazonaws", StringComparison.OrdinalIgnoreCase))
                yield return abs;
            yield break;
        }

        // očekujemo samo ime datoteke, npr. "TCGA-BRCA.htseq_fpkm-uq.tsv.gz"
        var f = given.Trim();

        // **ISKLJUČIVO Xena hub – path varijanta**
        yield return new Uri($"https://tcga.xenahubs.net/download/{Uri.EscapeDataString(f)}");

        // Ako baš želiš i query varijantu, možeš odkomentirati,
        // ali path radi pouzdanije:
        // yield return new Uri($"https://tcga.xenahubs.net/download?filename={Uri.EscapeDataString(f)}");
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OncoWeb.Models;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace OncoWeb.Controllers;

[ApiController]
[Route("api/clinical")]
public class ClinicalController : ControllerBase
{
    private readonly IAmazonS3 _s3;
    private readonly IOptions<AppOptions> _opts;
    private readonly IMongoCollection<ClinicalDoc> _col;

    public ClinicalController(IAmazonS3 s3, IOptions<AppOptions> opts, IMongoCollection<ClinicalDoc> col)
    {
        _s3 = s3; _opts = opts; _col = col;
    }

    private static int ColIndex(string[] hdr, params string[] names)
    {
        var dict = hdr.Select((h, i) => (h.Trim().ToLowerInvariant(), i))
                      .ToDictionary(x => x.Item1, x => x.i);
        foreach (var n in names)
        {
            if (dict.TryGetValue(n.Trim().ToLowerInvariant(), out var ix))
                return ix;
        }
        return -1;
    }

    private static int? Parse01(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        if (t.Equals("NA", StringComparison.OrdinalIgnoreCase)) return null;

        var first = t.Split(':')[0];
        if (int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            return v == 0 ? 0 : 1;

        if (string.Equals(t, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t, "yes", StringComparison.OrdinalIgnoreCase))
            return 1;

        if (string.Equals(t, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t, "no", StringComparison.OrdinalIgnoreCase))
            return 0;

        return null;
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string cohort, [FromQuery] string? objectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cohort))
            return BadRequest("cohort je obavezan.");

        var bucket = _opts.Value.Minio.Bucket;
        string key = objectName!;

        if (string.IsNullOrWhiteSpace(key))
        {

            var list = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = cohort.ToLowerInvariant() + "/"
            }, ct);

            key = list.S3Objects.OrderByDescending(o => o.LastModified)
                                .Select(o => o.Key)
                                .FirstOrDefault(k => k.Contains("clinical", StringComparison.OrdinalIgnoreCase));
            if (key == null) return BadRequest("Nema kliničkog TSV-a u bucketu za zadani cohort.");
        }

        using var obj = await _s3.GetObjectAsync(bucket, key, ct);
        await using var raw = obj.ResponseStream;

        using var ms = new MemoryStream();
        await raw.CopyToAsync(ms, ct);
        ms.Position = 0;

        Stream src = ms;
        if (key.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            src = new GZipStream(ms, CompressionMode.Decompress, leaveOpen: true);

        using var sr = new StreamReader(src, Encoding.UTF8, true, 1 << 16);

        var headerLine = await sr.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine)) return Ok(new { upserted = 0 });

        var header = headerLine.Split('\t');
        int ixBar = ColIndex(header, "bcr_patient_barcode", "patient_barcode", "barcode");
        int ixDSS = ColIndex(header, "dss", "dss.event", "dss_status");
        int ixOS = ColIndex(header, "os", "os.event", "os_status");
        int ixStage = ColIndex(header, "clinical_stage", "pathologic_stage", "ajcc_pathologic_tumor_stage");

        if (ixBar < 0) return BadRequest("Nedostaje stupac bcr_patient_barcode u TSV-u.");

        var writes = new List<WriteModel<ClinicalDoc>>(capacity: 2000);
        int upserted = 0;

        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('\t');
            if (parts.Length <= ixBar) continue;

            var bc = parts[ixBar].Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(bc)) continue;

            int? dss = ixDSS >= 0 && ixDSS < parts.Length ? Parse01(parts[ixDSS]) : null;
            int? os = ixOS >= 0 && ixOS < parts.Length ? Parse01(parts[ixOS]) : null;
            string? stage = (ixStage >= 0 && ixStage < parts.Length) ? parts[ixStage].Trim() : null;
            if (string.Equals(stage, "NA", StringComparison.OrdinalIgnoreCase)) stage = null;

            var filter = Builders<ClinicalDoc>.Filter.Where(d => d.PatientId == bc && d.CancerCohort == cohort);
            var update = Builders<ClinicalDoc>.Update
                .SetOnInsert(d => d.PatientId, bc)
                .SetOnInsert(d => d.CancerCohort, cohort)
                .Set(d => d.DSS, dss)
                .Set(d => d.OS, os)
                .Set(d => d.ClinicalStage, stage);

            writes.Add(new UpdateOneModel<ClinicalDoc>(filter, update) { IsUpsert = true });

            if (writes.Count >= 2000)
            {
                var res = await _col.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, ct);
                upserted += (int)(res.Upserts?.Count ?? 0) + (int)res.ModifiedCount;
                writes.Clear();
            }
        }

        if (writes.Count > 0)
        {
            var res = await _col.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, ct);
            upserted += (int)(res.Upserts?.Count ?? 0) + (int)res.ModifiedCount;
        }

        return Ok(new { upserted });
    }

    [HttpGet("{cohort}/{patientId}")]
    public async Task<ActionResult<ClinicalDoc>> GetOne(string cohort, string patientId, CancellationToken ct)
    {
        var doc = await _col.Find(d => d.CancerCohort == cohort && d.PatientId == patientId.ToUpperInvariant())
                            .FirstOrDefaultAsync(ct);
        return doc is null ? NotFound() : Ok(doc);
    }
}

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using OncoWeb.Models;
using OncoWeb.Services;
using System.Globalization;
using System.Text;

namespace OncoWeb.Controllers;

[ApiController]
[Route("api/genes")]
public class GenesController : ControllerBase
{
    private readonly GeneImportService _import;
    private readonly IMongoCollection<GeneExpressionDoc> _col;
    private readonly Microsoft.Extensions.Options.IOptions<AppOptions> _opts;
    private readonly IAmazonS3 _s3;

    public GenesController(GeneImportService import, IMongoCollection<GeneExpressionDoc> col, IOptions<AppOptions> opts, IAmazonS3 s3)
    {
        _import = import;
        _col = col;
        _opts = opts;
        _s3 = s3;
    }

    private string[] GetPanelGenes()
    {
        var s = new HashSet<string>(_opts.Value.Genes.Select(g => g.ToUpperInvariant()));
        s.Remove("IL8"); s.Add("CXCL8");
        return s.ToArray();
    }

    [HttpGet("{cohort}/patients")]
    public async Task<ActionResult<object>> ListPatients(
    string cohort, [FromQuery] string? prefix, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (take is < 1 or > 500) take = 50;
        var q = _col.AsQueryable().Where(d => d.CancerCohort == cohort);
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            var p = prefix.ToUpperInvariant();
            q = q.Where(d => d.PatientId.StartsWith(p));
        }

        var total = await q.CountAsync(ct);
        var ids = await q.Select(d => d.PatientId)
                         .OrderBy(id => id)
                         .Skip(skip).Take(take)
                         .ToListAsync(ct);

        return Ok(new { total, items = ids });
    }


    [HttpPost("import")]
    public async Task<IActionResult> Import([FromQuery] string cohort,
                                        [FromQuery] string? objectName,
                                        CancellationToken ct)
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
                                .FirstOrDefault()?.Key
                  ?? throw new InvalidOperationException("Nema objekata za zadani cohort.");
        }

        using var obj = await _s3.GetObjectAsync(bucket, key, ct);
        await using var net = obj.ResponseStream;
        using var buffer = new MemoryStream(capacity: 1 << 20); 
        await net.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        Stream payload = buffer;
        if (key.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            payload = new System.IO.Compression.GZipStream(buffer,
                        System.IO.Compression.CompressionMode.Decompress, leaveOpen: true);

        using var sr = new StreamReader(payload, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1 << 16);

        var wanted = new HashSet<string>(_opts.Value.Genes.Select(g => g.ToUpperInvariant()));
        wanted.Remove("IL8"); wanted.Add("CXCL8");

        var header = (await sr.ReadLineAsync())?.Split('\t') ?? Array.Empty<string>();
        if (header.Length < 2) return Ok(new { upserted = 0 });

        var samples = header.Skip(1)
                            .Select(h => (raw: h, bc: (h.Length >= 12 ? h[..12] : h).ToUpperInvariant()))
                            .ToArray();

        var writes = new List<WriteModel<GeneExpressionDoc>>(capacity: 5000);
        int upserted = 0;

        string? line;
        while ((line = await sr.ReadLineAsync()) != null)
        {
            var parts = line.Split('\t');
            if (parts.Length < 2) continue;

            var geneOrig = parts[0].Trim();
            var geneKey = geneOrig.ToUpperInvariant() == "IL8" ? "CXCL8" : geneOrig.ToUpperInvariant();
            if (!wanted.Contains(geneKey)) continue;

            for (int i = 0; i < samples.Length; i++)
            {
                var valStr = i + 1 < parts.Length ? parts[i + 1] : "";
                if (string.IsNullOrWhiteSpace(valStr) || valStr == "NA") continue;

                if (!double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    continue;

                var bc = samples[i].bc;
                var filter = Builders<GeneExpressionDoc>.Filter.Where(d => d.PatientId == bc && d.CancerCohort == cohort);
                var update = Builders<GeneExpressionDoc>.Update
                    .SetOnInsert(d => d.PatientId, bc)
                    .SetOnInsert(d => d.CancerCohort, cohort)
                    .Set($"Genes.{geneKey}", val);

                writes.Add(new UpdateOneModel<GeneExpressionDoc>(filter, update) { IsUpsert = true });

                if (writes.Count >= 2000)
                {
                    var res = await _col.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, ct);
                    upserted += (int)(res.Upserts?.Count ?? 0) + (int)(res.ModifiedCount);
                    writes.Clear();
                }
            }
        }

        if (writes.Count > 0)
        {
            var res = await _col.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, ct);
            upserted += (int)(res.Upserts?.Count ?? 0) + (int)(res.ModifiedCount);
        }

        return Ok(new { upserted });
    }


    [HttpGet("{cohort}/panel/{patientId}")]
    public async Task<ActionResult<object>> GetPanel(string cohort, string patientId, CancellationToken ct)
    {
        var panel = GetPanelGenes();
        var proj = Builders<GeneExpressionDoc>.Projection
            .Include(d => d.PatientId)
            .Include(d => d.CancerCohort)
            .Include(d => d.Genes);
        var doc = await _col.Find(d => d.CancerCohort == cohort && d.PatientId == patientId)
                            .Project<GeneExpressionDoc>(proj)
                            .FirstOrDefaultAsync(ct);
        if (doc is null) return NotFound();

        var dict = new Dictionary<string, double>();
        foreach (var g in panel)
            if (doc.Genes.TryGetValue(g, out var v)) dict[g] = v;

        return Ok(new
        {
            doc.PatientId,
            doc.CancerCohort,
            Genes = dict   
        });
    }

    [HttpGet("{cohort}/gene/{gene}/top")]
    public async Task<ActionResult<List<object>>> TopByGene(
      string cohort, string gene, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (limit is < 1 or > 200) limit = 20;
        var key = gene.ToUpperInvariant() == "IL8" ? "CXCL8" : gene.ToUpperInvariant();

        var filter = Builders<GeneExpressionDoc>.Filter.And(
            Builders<GeneExpressionDoc>.Filter.Eq(d => d.CancerCohort, cohort),
            Builders<GeneExpressionDoc>.Filter.Exists($"Genes.{key}", true)
        );

        var proj = Builders<GeneExpressionDoc>.Projection
            .Include(d => d.PatientId)
            .Include($"Genes.{key}");

        var docs = await _col.Find(filter)
            .Sort(Builders<GeneExpressionDoc>.Sort.Descending($"Genes.{key}")) 
            .Project<BsonDocument>(proj)
            .Limit(limit)
            .ToListAsync(ct);

        var result = docs.Select(d => new
        {
            PatientId = d["PatientId"].AsString,
            Value = d["Genes"].AsBsonDocument.GetValue(key, BsonNull.Value).ToDouble()
        }).ToList();

        return Ok(result);
    }

}

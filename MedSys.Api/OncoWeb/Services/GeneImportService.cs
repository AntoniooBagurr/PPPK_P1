using System.IO.Compression;
using System.Text;
using MongoDB.Driver;
using OncoWeb.Models;

namespace OncoWeb.Services;

public class GeneImportService
{
    private readonly IStorageService _storage;
    private readonly IMongoCollection<GeneExpressionDoc> _col;
    private readonly AppOptions _opt;
    private readonly ILogger<GeneImportService> _log;

    public GeneImportService(
        IStorageService storage,
        IMongoCollection<GeneExpressionDoc> col,
        Microsoft.Extensions.Options.IOptions<AppOptions> opt,
        ILogger<GeneImportService> log)
    {
        _storage = storage;
        _col = col;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<int> ImportCohortAsync(string cohort, string? objectName = null, CancellationToken ct = default)
    {
        var bucket = _opt.Minio.Bucket;
        var prefix = cohort.ToLowerInvariant() + "/";

        var keys = objectName is null
            ? await _storage.ListAsync(bucket, prefix, ct)
            : new List<string> { objectName.StartsWith(prefix) ? objectName : prefix + objectName };

        keys = keys.Where(k =>
                k.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(".tsv.gz", StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(".xena", StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith(".xena.gz", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (keys.Count == 0) return 0;

        int upserts = 0;
        foreach (var key in keys)
        {
            using var stream = await _storage.GetObjectStreamAsync(bucket, key, ct);

            Stream data = key.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(stream, CompressionMode.Decompress, leaveOpen: false)
                : stream;

            using var sr = new StreamReader(data, Encoding.UTF8, true, 1 << 16);

            upserts += await ParseAndUpsertAsync(sr, cohort, ct);
        }

        return upserts;
    }

    private static string CleanGene(string g)
    {
        if (string.IsNullOrWhiteSpace(g)) return g;
        var i = g.IndexOf('|');
        return i > 0 ? g[..i] : g;
    }

    private static string ToPatientBarcode(string sampleOrBarcode)
    {

        var parts = sampleOrBarcode.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 ? $"{parts[0]}-{parts[1]}-{parts[2]}" : sampleOrBarcode;
    }

    private async Task<int> ParseAndUpsertAsync(StreamReader sr, string cohort, CancellationToken ct)
    {
        var header = await sr.ReadLineAsync() ?? "";
        var cols = header.Split('\t');

        var target = _opt.Genes.Select(CleanGene).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int upserts = 0;

        if (cols.Length > 1 && cols[0].Equals("sample", StringComparison.OrdinalIgnoreCase))
        {
            var geneIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < cols.Length; i++)
            {
                var g = CleanGene(cols[i]);
                if (target.Contains(g)) geneIndex[g] = i;
            }

            string? line;
            while ((line = await sr.ReadLineAsync()) is not null)
            {
                var c = line.Split('\t');
                if (c.Length < 2) continue;

                var patient = ToPatientBarcode(c[0]);
                var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                foreach (var (gene, idx) in geneIndex)
                {
                    if (idx < c.Length && double.TryParse(c[idx], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                        dict[gene] = val;
                }

                if (dict.Count > 0)
                    upserts += await UpsertAsync(patient, cohort, dict, ct);
            }
        }
        else
        {
          
            var patientAt = new List<string>();
            for (int i = 1; i < cols.Length; i++)
                patientAt.Add(ToPatientBarcode(cols[i]));

            var acc = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

            string? line;
            while ((line = await sr.ReadLineAsync()) is not null)
            {
                var c = line.Split('\t');
                if (c.Length < 2) continue;

                var gene = CleanGene(c[0]);
                if (!target.Contains(gene)) continue;

                for (int i = 1; i < c.Length && i <= patientAt.Count; i++)
                {
                    if (double.TryParse(c[i], System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var val))
                    {
                        var p = patientAt[i - 1];
                        if (!acc.TryGetValue(p, out var gdict))
                            acc[p] = gdict = new(StringComparer.OrdinalIgnoreCase);
                        gdict[gene] = val;
                    }
                }
            }

            foreach (var (patient, gdict) in acc)
                upserts += await UpsertAsync(patient, cohort, gdict, ct);
        }

        return upserts;
    }

    private async Task<int> UpsertAsync(string patient, string cohort, Dictionary<string, double> genes, CancellationToken ct)
    {
        var filter = Builders<GeneExpressionDoc>.Filter.Where(d => d.PatientId == patient && d.CancerCohort == cohort);
        var update = Builders<GeneExpressionDoc>.Update
            .Set(d => d.PatientId, patient)
            .Set(d => d.CancerCohort, cohort)
            .Set(d => d.Genes, genes);

        var opts = new UpdateOptions { IsUpsert = true };
        var res = await _col.UpdateOneAsync(filter, update, opts, ct);
        return (res.UpsertedId != null || res.ModifiedCount > 0) ? 1 : 0;
    }
}

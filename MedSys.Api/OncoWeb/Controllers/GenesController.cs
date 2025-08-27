using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public record GeneQuery(string Cohort, string[] Patients, string[]? Genes);

[ApiController]
[Route("api/genes")]
public class GenesController : ControllerBase
{
    private readonly IMongoCollection<GeneExpressionDoc> _col;
    private readonly string[] _defaultGenes;

    public GenesController(IMongoCollection<GeneExpressionDoc> col, IOptions<AppOptions> app)
    {
        _col = col;
        _defaultGenes = app.Value.Genes;
    }


    [HttpGet("{cohort}/{patientId}")]
    public async Task<ActionResult<GeneExpressionDoc>> GetOne(string cohort, string patientId)
    {
        var filter = Builders<GeneExpressionDoc>.Filter.Eq(x => x.CancerCohort, cohort) &
                     Builders<GeneExpressionDoc>.Filter.Eq(x => x.PatientId, patientId);
        var doc = await _col.Find(filter).FirstOrDefaultAsync();
        if (doc == null) return NotFound();

        var subset = doc.Expressions
            .Where(kv => _defaultGenes.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        doc.Expressions = subset;
        return Ok(doc);
    }


    [HttpPost("query")]
    public async Task<ActionResult<object>> Query([FromBody] GeneQuery q)
    {
        if (q == null || string.IsNullOrWhiteSpace(q.Cohort) || (q.Patients?.Length ?? 0) == 0)
            return BadRequest();

        var filter = Builders<GeneExpressionDoc>.Filter.Eq(x => x.CancerCohort, q.Cohort) &
                     Builders<GeneExpressionDoc>.Filter.In(x => x.PatientId, q.Patients);

        var docs = await _col.Find(filter).ToListAsync();

        var genes = (q.Genes?.Length ?? 0) > 0 ? q.Genes! : _defaultGenes;

        var shaped = docs.Select(d =>
            new
            {
                patientId = d.PatientId,
                cohort = d.CancerCohort,
                expressions = genes.ToDictionary(
                    g => g,
                    g => d.Expressions.TryGetValue(g, out var v) ? v : double.NaN,
                    StringComparer.OrdinalIgnoreCase)
            });

        return Ok(shaped);
    }
}

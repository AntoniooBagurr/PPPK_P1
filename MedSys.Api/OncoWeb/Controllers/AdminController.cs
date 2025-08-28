using Microsoft.AspNetCore.Mvc;
using OncoWeb.Models;
using OncoWeb.Services;

namespace OncoWeb.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IngestService _svc;
    private readonly IStorageService _storage;
    private readonly Microsoft.Extensions.Options.IOptions<AppOptions> _opts;

    public AdminController(
        IngestService svc,
        IStorageService storage,
        Microsoft.Extensions.Options.IOptions<AppOptions> opts)
    {
        _svc = svc; _storage = storage; _opts = opts;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestRequest req, CancellationToken ct)
    {
        if (req?.Jobs == null || req.Jobs.Count == 0)
            return BadRequest("jobs je obavezan.");

        var result = await _svc.RunAsync(req, ct);
        if (result.Errors.Count == req.Jobs.Count && result.Downloaded.Count == 0)
            return StatusCode(502, result);

        return Ok(result);
    }

 
    [HttpPost("ingest/local")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> IngestLocal([FromForm] IngestLocalForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Cohort))
            return BadRequest("cohort je obavezan.");
        if (form.File == null || form.File.Length == 0)
            return BadRequest("file je obavezan.");

        var bucket = _opts.Value.Minio.Bucket;
        await _storage.EnsureBucketAsync(bucket, ct);

        var objectName = $"{form.Cohort.ToLowerInvariant()}/{Path.GetFileName(form.File.FileName)}";
       
        await using var s = form.File.OpenReadStream();
        await _storage.PutObjectAsync(
            bucket,
            objectName,
            s,
            form.File.ContentType ?? "application/octet-stream",
            ct);

        return Ok(new { saved = objectName, bytes = form.File.Length });
    }
}

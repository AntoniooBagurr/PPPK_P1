using Microsoft.AspNetCore.Mvc;
using OncoWeb.Models;
using OncoWeb.Services;

namespace OncoWeb.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IngestService _svc;

    public AdminController(IngestService svc) => _svc = svc;

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestRequest req, CancellationToken ct)
    {
        if (req?.Jobs == null || req.Jobs.Count == 0)
            return BadRequest("jobs je obavezan.");

        var result = await _svc.RunAsync(req, ct);
        return Ok(result);
    }

    // POST /api/admin/ingest/single
    [HttpPost("ingest/single")]
    public async Task<IActionResult> IngestSingle([FromBody] IngestJob job, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(job?.Cohort) || string.IsNullOrWhiteSpace(job.Url))
            return BadRequest("cohort i url su obavezni.");

        var result = await _svc.RunAsync(new IngestRequest { Jobs = new() { job } }, ct);
        return Ok(result);
    }

    [HttpPost("ingest/local")]
    public async Task<IActionResult> IngestLocal([FromForm] string cohort, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cohort)) return BadRequest("cohort je obavezan.");
        if (file == null || file.Length == 0) return BadRequest("file je obavezan.");

        var objectName = $"{cohort.ToLowerInvariant()}/{Path.GetFileName(file.FileName)}";
        await _svc.UploadLocalAsync(objectName, file.ContentType ?? "application/octet-stream", file.OpenReadStream(), file.Length, ct);
        return Ok(new { uploaded = objectName, bytes = file.Length });
    }

}

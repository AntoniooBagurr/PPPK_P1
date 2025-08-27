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
        if (req == null || string.IsNullOrWhiteSpace(req.Cohort))
            return BadRequest("cohort je obavezan.");

        if (req.TsvUrls == null || req.TsvUrls.Count == 0) // Count PROPERTY na List<string>
            return BadRequest("tsvUrls mora sadržavati barem jedan URL.");

        // eksplicitna validacija da izbjegnemo 500 i UriFormatException
        for (int i = 0; i < req.TsvUrls.Count; i++)
        {
            var raw = (req.TsvUrls[i] ?? string.Empty).Trim();
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return BadRequest($"Neispravan URL na indeksu {i}: '{raw}'. Očekujem apsolutni http(s) URL.");
        }

        var result = await _svc.RunAsync(req, ct);
        return Ok(result);
    }
}

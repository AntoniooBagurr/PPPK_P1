using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using MedSys.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/visits/{visitId:guid}/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly AppDb _db;
    private readonly IStorageService _storage;
    public DocumentsController(AppDb db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    // POST /api/visits/{visitId}/documents
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(Guid visitId, [FromForm] IFormFile file, CancellationToken ct)
    {
        var visit = await _db.Visits.FindAsync(visitId);
        if (visit is null) return NotFound("Pregled ne postoji.");
        if (file is null || file.Length == 0) return BadRequest("Prazna datoteka.");

        var key = $"visits/{visitId}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        await using var s = file.OpenReadStream();
        var url = await _storage.UploadAsync(s, file.ContentType, key, ct);

        var doc = new Document
        {
            VisitId = visitId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StorageUrl = url
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return Ok(new { doc.Id, doc.FileName, doc.ContentType, doc.SizeBytes, doc.StorageUrl, doc.UploadedAt });
    }

    // GET /api/visits/{visitId}/documents
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromRoute] Guid visitId)
    {
        var docs = await _db.Documents
            .Where(d => d.VisitId == visitId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.StorageUrl, d.UploadedAt))
            .ToListAsync();

        return Ok(docs);
    }
}

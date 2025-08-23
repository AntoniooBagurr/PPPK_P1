using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/visits/{visitId:guid}/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly AppDb _db;
    public DocumentsController(AppDb db) => _db = db;

    // POST /api/visits/{visitId}/documents
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)] // 50 MB
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload([FromRoute] Guid visitId, [FromForm] IFormFile file)
    {
        var visit = await _db.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == visitId);
        if (visit is null) return NotFound("Pregled ne postoji.");
        if (file is null || file.Length == 0) return BadRequest("Prazna datoteka.");

        // TODO: stvarni upload u Supabase/S3 → dohvati URL
        var fakeUrl = $"https://storage.example/visits/{visitId}/{file.FileName}";

        var doc = new Document
        {
            VisitId = visitId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StorageUrl = fakeUrl
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var dto = new DocumentDto(doc.Id, doc.FileName, doc.ContentType, doc.SizeBytes, doc.StorageUrl, doc.UploadedAt);
        return Ok(dto);
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

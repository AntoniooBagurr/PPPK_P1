using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using MedSys.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/patients/{patientId:guid}/documents")]
[Produces("application/json")]
public class PatientDocumentsController : ControllerBase
{
    private readonly AppDb _db;
    private readonly IStorageService _storage;

    public PatientDocumentsController(AppDb db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    // POST /api/patients/{patientId}/documents
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(Guid patientId, [FromForm] IFormFile file, CancellationToken ct)
    {
        var patient = await _db.Patients.FindAsync(patientId);
        if (patient is null) return NotFound("Pacijent ne postoji.");
        if (file is null || file.Length == 0) return BadRequest("Prazna datoteka.");

        var key = $"patients/{patientId}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        await using var s = file.OpenReadStream();
        var url = await _storage.UploadAsync(s, file.ContentType ?? "application/octet-stream", key, ct);

        var doc = new Document
        {
            PatientId = patientId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StorageUrl = url
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return Ok(new DocumentDto(doc.Id, doc.FileName, doc.ContentType, doc.SizeBytes, doc.StorageUrl, doc.UploadedAt));
    }

    // GET /api/patients/{patientId}/documents
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid patientId)
    {
        var docs = await _db.Documents
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.StorageUrl, d.UploadedAt))
            .ToListAsync();

        return Ok(docs);
    }
}

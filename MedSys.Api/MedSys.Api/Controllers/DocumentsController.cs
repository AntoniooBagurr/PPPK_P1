using MedSys.Api.Data;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/visits/{visitId:guid}/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDb _db;
    public DocumentsController(AppDb db) => _db = db;

    // Najjednostavnije: primi file i spremi StorageUrl koji dobiješ iz storage servisa (TODO Day 3)
    [HttpPost]
    [RequestSizeLimit(50_000_000)] // 50 MB
    public async Task<IActionResult> Upload(Guid visitId, IFormFile file)
    {
        var visit = await _db.Visits.FindAsync(visitId);
        if (visit == null) return NotFound("Pregled ne postoji.");

        if (file == null || file.Length == 0) return BadRequest("Prazna datoteka.");
        // TODO: upload u Supabase/S3 i dohvati public/signed URL
        var fakeUrl = $"https://storage.example/visits/{visitId}/{file.FileName}";

        var doc = new Document
        {
            VisitId = visitId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StorageUrl = fakeUrl
        };
        await _db.Documents.AddAsync(doc);
        await _db.SaveChangesAsync();
        return Ok(new { doc.Id, doc.FileName, doc.StorageUrl });
    }

    [HttpGet]
    public IActionResult List(Guid visitId)
    {
        var docs = _db.Documents.Where(d => d.VisitId == visitId)
            .Select(d => new { d.Id, d.FileName, d.ContentType, d.SizeBytes, d.StorageUrl, d.UploadedAt })
            .ToList();
        return Ok(docs);
    }
}

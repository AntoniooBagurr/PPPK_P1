using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/patients/{patientId:guid}/medicalhistory")]
public class MedicalHistoryController : ControllerBase
{
    private readonly AppDb _db;
    public MedicalHistoryController(AppDb db) => _db = db;

    // helper: Unspecified -> UTC date (bez vremena)
    private static DateTime AsUtcDate(DateTime d)
        => DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);

    // GET /api/patients/{patientId}/medicalhistory
    [HttpGet]
    public async Task<IActionResult> List(Guid patientId)
    {
        var exists = await _db.Patients.AnyAsync(p => p.Id == patientId);
        if (!exists) return NotFound("Pacijent ne postoji.");

        var items = await _db.MedicalHistory
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new MedicalHistoryItemDto(x.Id, x.DiseaseName, x.StartDate, x.EndDate))
            .ToListAsync();

        return Ok(items);
    }

    // POST /api/patients/{patientId}/medicalhistory
    [HttpPost]
    public async Task<IActionResult> Create(Guid patientId, [FromBody] MedicalHistoryDto dto)
    {
        var p = await _db.Patients.FindAsync(patientId);
        if (p is null) return NotFound("Pacijent ne postoji.");

        var mh = new MedicalHistory
        {
            PatientId = patientId,
            DiseaseName = dto.DiseaseName.Trim(),
            StartDate = AsUtcDate(dto.StartDate),
            EndDate = dto.EndDate.HasValue ? AsUtcDate(dto.EndDate.Value) : (DateTime?)null
        };

        await _db.MedicalHistory.AddAsync(mh);
        await _db.SaveChangesAsync();

        return Ok(new MedicalHistoryItemDto(mh.Id, mh.DiseaseName, mh.StartDate, mh.EndDate));
    }

    // PUT /api/patients/{patientId}/medicalhistory/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid patientId, Guid id, [FromBody] MedicalHistoryDto dto)
    {
        var mh = await _db.MedicalHistory.FirstOrDefaultAsync(x => x.Id == id && x.PatientId == patientId);
        if (mh is null) return NotFound();

        mh.DiseaseName = dto.DiseaseName.Trim();
        mh.StartDate = AsUtcDate(dto.StartDate);
        mh.EndDate = dto.EndDate.HasValue ? AsUtcDate(dto.EndDate.Value) : null;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/patients/{patientId}/medicalhistory/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid patientId, Guid id)
    {
        var mh = await _db.MedicalHistory.FirstOrDefaultAsync(x => x.Id == id && x.PatientId == patientId);
        if (mh is null) return NotFound();

        _db.MedicalHistory.Remove(mh);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

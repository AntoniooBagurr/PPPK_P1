using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/patients/{patientId:guid}/[controller]")]
public class MedicalHistoryController : ControllerBase
{
    private readonly AppDb _db;
    public MedicalHistoryController(AppDb db) => _db = db;

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

    [HttpPost]
    public async Task<IActionResult> Create(Guid patientId, [FromBody] MedicalHistoryDto dto)
    {
        if (patientId != dto.PatientId) return BadRequest("Path i body patientId moraju se poklapati.");
        if (!await _db.Patients.AnyAsync(p => p.Id == patientId)) return NotFound("Pacijent ne postoji.");

        var entity = new MedicalHistory
        {
            PatientId = patientId,
            DiseaseName = dto.DiseaseName,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
        _db.MedicalHistory.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(List), new { patientId }, new MedicalHistoryItemDto(entity.Id, entity.DiseaseName, entity.StartDate, entity.EndDate));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid patientId, Guid id, [FromBody] MedicalHistoryDto dto)
    {
        var mh = await _db.MedicalHistory.FirstOrDefaultAsync(x => x.Id == id && x.PatientId == patientId);
        if (mh is null) return NotFound();

        mh.DiseaseName = dto.DiseaseName;
        mh.StartDate = dto.StartDate;
        mh.EndDate = dto.EndDate;
        await _db.SaveChangesAsync();
        return NoContent();
    }

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

using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/visits/{visitId:guid}/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDb _db;
    public PrescriptionsController(AppDb db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(Guid visitId)
    {
        var exists = await _db.Visits.AnyAsync(v => v.Id == visitId);
        if (!exists) return NotFound("Pregled ne postoji.");

        var data = await _db.Prescriptions
            .Where(p => p.VisitId == visitId)
            .Select(p => new PrescriptionReadDto(
                p.Id, p.IssuedAt, p.Notes,
                p.Items.Select(i => new PrescriptionItemReadDto(i.Dosage, i.Frequency, i.DurationDays, i.Medication.Name)).ToList()))
            .ToListAsync();

        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid visitId, [FromBody] PrescriptionCreateDto dto)
    {
        if (!await _db.Visits.AnyAsync(v => v.Id == visitId)) return NotFound("Pregled ne postoji.");

        var p = new Prescription { VisitId = visitId, Notes = dto.Notes };
        _db.Prescriptions.Add(p);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new { visitId }, new { p.Id, p.VisitId, p.IssuedAt, p.Notes });
    }

    // /api/visits/{visitId}/prescriptions/{id}/items
    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid visitId, Guid id, [FromBody] PrescriptionItemCreateDto dto)
    {
        var p = await _db.Prescriptions.FirstOrDefaultAsync(x => x.Id == id && x.VisitId == visitId);
        if (p is null) return NotFound("Recept ne postoji.");
        if (!await _db.Medications.AnyAsync(m => m.Id == dto.MedicationId))
            return BadRequest("Lijek ne postoji.");

        var item = new PrescriptionItem
        {
            PrescriptionId = id,
            MedicationId = dto.MedicationId,
            Dosage = dto.Dosage,
            Frequency = dto.Frequency,
            DurationDays = dto.DurationDays
        };
        _db.PrescriptionItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { item.Id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid visitId, Guid id)
    {
        var p = await _db.Prescriptions.FirstOrDefaultAsync(x => x.Id == id && x.VisitId == visitId);
        if (p is null) return NotFound();
        _db.Prescriptions.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

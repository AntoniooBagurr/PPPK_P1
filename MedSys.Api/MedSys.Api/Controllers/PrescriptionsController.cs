using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly AppDb _db;
    public PrescriptionsController(AppDb db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrescriptionCreateDto dto)
    {
        var visit = await _db.Visits.FindAsync(dto.VisitId);
        if (visit is null) return BadRequest("Pregled ne postoji.");

        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest("Recept mora imati barem jednu stavku.");

        var pr = new Prescription { VisitId = dto.VisitId, Notes = dto.Notes };
        await _db.Prescriptions.AddAsync(pr);

        foreach (var item in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(item.MedicationName))
                return BadRequest("MedicationName je obavezan.");

            var name = item.MedicationName.Trim();

            // upsert po nazivu (case-insensitive)
            var med = await _db.Medications
                .FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower());

            if (med is null)
            {
                med = new Medication { Name = name };
                await _db.Medications.AddAsync(med);
            }

            await _db.PrescriptionItems.AddAsync(new PrescriptionItem
            {
                Prescription = pr,
                Medication = med,
                Dosage = item.Dosage?.Trim() ?? "",
                Frequency = item.Frequency?.Trim() ?? "",
                DurationDays = item.DurationDays
            });
        }

        await _db.SaveChangesAsync();

        // vrati DTO za UI
        var items = await _db.PrescriptionItems
            .Where(i => i.PrescriptionId == pr.Id)
            .Include(i => i.Medication)
            .Select(i => new PrescriptionItemReadDto(
                i.Dosage, i.Frequency, i.DurationDays, i.Medication.Name))
            .ToListAsync();

        var read = new PrescriptionReadDto(pr.Id, pr.IssuedAt, pr.Notes, items);
        return Ok(read);
    }
}

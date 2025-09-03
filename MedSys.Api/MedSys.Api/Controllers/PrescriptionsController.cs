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
        var visit = await _db.Visits.FirstOrDefaultAsync(v => v.Id == dto.VisitId);
        if (visit is null) return BadRequest("Pregled (visit) ne postoji.");

        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest("Recept mora imati barem jednu stavku.");

        var pr = new Prescription
        {
            VisitId = dto.VisitId,
            IssuedAt = DateTimeOffset.UtcNow,
            Notes = dto.Notes
        };

        foreach (var it in dto.Items)
        {
            Medication? med = null;

            if (it.MedicationId.HasValue)
            {
                med = await _db.Medications.FindAsync(it.MedicationId.Value);
                if (med is null) return BadRequest("Nepostojeći MedicationId.");
            }
            else if (!string.IsNullOrWhiteSpace(it.MedicationName))
            {
                var name = it.MedicationName.Trim();
                med = await _db.Medications.FirstOrDefaultAsync(m => m.Name == name);
                if (med is null)
                {
                    med = new Medication { Name = name };
                    _db.Medications.Add(med);
                    await _db.SaveChangesAsync();
                }
            }
            else return BadRequest("Nedostaje lijek (MedicationId ili MedicationName).");

            pr.Items.Add(new PrescriptionItem
            {
                MedicationId = med!.Id,
                Dosage = it.Dosage,
                Frequency = it.Frequency,
                DurationDays = it.DurationDays
            });
        }

        _db.Prescriptions.Add(pr);
        await _db.SaveChangesAsync();

        return Ok(new { pr.Id });
    }
}

using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicationsController : ControllerBase
{
    private readonly AppDb _db;
    public MedicationsController(AppDb db) => _db = db;

    // GET /api/medications?q=amo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicationReadDto>>> Search([FromQuery] string? q)
    {
        var query = _db.Medications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(m => EF.Functions.ILike(m.Name, q.Trim() + "%"));

        var list = await query
            .OrderBy(m => m.Name)
            .Take(20)
            .Select(m => new MedicationReadDto(m.Id, m.Name, m.AtcCode))
            .ToListAsync();

        return Ok(list);
    }

    // (opcionalno) POST /api/medications  – ručno dodavanje jednog lijeka
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedicationCreateDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required.");

        var exists = await _db.Medications.AnyAsync(m => m.Name == name);
        if (exists) return Conflict("Medication with same name already exists.");

        var med = new Medication { Name = name, AtcCode = dto.AtcCode?.Trim() };
        _db.Medications.Add(med);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Search), new { q = med.Name }, new MedicationReadDto(med.Id, med.Name, med.AtcCode));
    }

    // (opcionalno) brzi seed ako baza nema lijekove
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (await _db.Medications.AnyAsync()) return Ok(new { seeded = false });

        var meds = new[]
        {
            new Medication { Name = "Paracetamol", AtcCode = "N02BE01" },
            new Medication { Name = "Ibuprofen", AtcCode = "M01AE01" },
            new Medication { Name = "Amoksicilin", AtcCode = "J01CA04" },
            new Medication { Name = "Azitromicin", AtcCode = "J01FA10" },
            new Medication { Name = "Loratadin", AtcCode = "R06AX13" },
            new Medication { Name = "Omeprazol", AtcCode = "A02BC01" }
        };
        _db.Medications.AddRange(meds);
        await _db.SaveChangesAsync();
        return Ok(new { seeded = true, count = meds.Length });
    }



    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Medication input)
    {
        var m = await _db.Medications.FindAsync(id);
        if (m is null) return NotFound();

        if (string.IsNullOrWhiteSpace(input.Name))
            return BadRequest("Name is required.");

        m.Name = input.Name.Trim();
        m.AtcCode = input.AtcCode?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await _db.Medications.FindAsync(id);
        if (m is null) return NotFound();
        _db.Medications.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

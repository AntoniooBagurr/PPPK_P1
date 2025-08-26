using MedSys.Api.Data;
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

    // CRUD je dostupan u Swaggeru

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Medication>>> GetAll()
        => Ok(await _db.Medications.AsNoTracking()
               .OrderBy(m => m.Name).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Medication>> Get(Guid id)
        => await _db.Medications.FindAsync(id) is { } m ? Ok(m) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Medication input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            return BadRequest("Name is required.");

        var m = new Medication { Name = input.Name.Trim(), AtcCode = input.AtcCode?.Trim() };
        await _db.Medications.AddAsync(m);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = m.Id }, m);
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

    // === Autocomplete za UI (pretraga po nazivu) ===
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        q = q.Trim();

        var list = await _db.Medications.AsNoTracking()
            .Where(m => EF.Functions.ILike(m.Name, q + "%"))
            .OrderBy(m => m.Name)
            .Take(20)
            .Select(m => new { m.Id, m.Name, m.AtcCode })
            .ToListAsync();

        return Ok(list);
    }
}

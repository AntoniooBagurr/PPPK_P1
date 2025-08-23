using MedSys.Api.Data;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    { "GP","KRV","X-RAY","CT","MR","ULTRA","EKG","ECHO","EYE","DERM","DENTA","MAMMO","NEURO" };

    private readonly AppDb _db;
    public VisitsController(AppDb db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Visit v)
    {
        if (!AllowedTypes.Contains(v.VisitType)) return BadRequest("Neispravan visit_type.");
        if (await _db.Patients.FindAsync(v.PatientId) is null) return BadRequest("Pacijent ne postoji.");

        await _db.Visits.AddAsync(v);
        await _db.SaveChangesAsync();
        return Ok(v);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var v = await _db.Visits.FindAsync(id);
        return v is null ? NotFound() : Ok(v);
    }
}

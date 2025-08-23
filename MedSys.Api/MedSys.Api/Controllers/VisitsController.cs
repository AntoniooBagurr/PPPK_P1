using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly AppDb _db;
    public VisitsController(AppDb db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VisitCreateDto dto)
    {
        if (await _db.Patients.FindAsync(dto.PatientId) is null)
            return BadRequest("Pacijent ne postoji.");

        var v = new Visit
        {
            PatientId = dto.PatientId,
            VisitDateTime = dto.VisitDateTime,
            VisitType = dto.VisitType,
            Notes = dto.Notes
        };

        await _db.Visits.AddAsync(v);
        await _db.SaveChangesAsync();
        return Ok(new { v.Id, v.PatientId, v.VisitDateTime, v.VisitType, v.Notes });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var v = await _db.Visits.FindAsync(id);
        return v is null ? NotFound() : Ok(new { v.Id, v.PatientId, v.VisitDateTime, v.VisitType, v.Notes });
    }
}

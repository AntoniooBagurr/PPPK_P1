using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;   
using System.Security.Claims;            

namespace MedSys.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly AppDb _db;
    public VisitsController(AppDb db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VisitCreateDto dto, CancellationToken ct)
    {
        if (await _db.Patients.FindAsync(new object[] { dto.PatientId }, ct) is null)
            return BadRequest("Pacijent ne postoji.");

        Guid? currentDoctorId = null;
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var parsed))
            currentDoctorId = parsed;

        var doctorId = currentDoctorId ?? dto.DoctorId;

        if (doctorId.HasValue &&
            await _db.Doctors.FindAsync(new object[] { doctorId.Value }, ct) is null)
            return BadRequest("Liječnik ne postoji.");

        var v = new Visit
        {
            PatientId = dto.PatientId,
            VisitDateTime = dto.VisitDateTime,
            VisitType = dto.VisitType,
            Notes = dto.Notes,
            DoctorId = doctorId
        };

        _db.Visits.Add(v);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            v.Id,
            v.PatientId,
            v.VisitDateTime,
            v.VisitType,
            v.Notes,
            v.DoctorId
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = await _db.Visits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null
            ? NotFound()
            : Ok(new { v.Id, v.PatientId, v.VisitDateTime, v.VisitType, v.Notes, v.DoctorId });
    }
}

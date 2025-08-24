using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly AppDb _db;
    public DoctorsController(AppDb db) => _db = db;

    // GET /api/doctors?name=&license=
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? name, [FromQuery] string? license)
    {
        var q = _db.Doctors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(d => EF.Functions.ILike(d.FullName, "%" + name + "%"));
        if (!string.IsNullOrWhiteSpace(license))
            q = q.Where(d => d.LicenseNo == license);

        var list = await q.OrderBy(d => d.FullName).ToListAsync();
        return Ok(list.Select(d => new { d.Id, d.FullName, d.LicenseNo, d.Email, d.Phone }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorDto dto)
    {
        var d = new Doctor
        {
            FullName = dto.FullName.Trim(),
            LicenseNo = string.IsNullOrWhiteSpace(dto.LicenseNo) ? null : dto.LicenseNo.Trim(),
            Email = dto.Email,
            Phone = dto.Phone
        };
        _db.Doctors.Add(d);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = d.Id }, d);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var d = await _db.Doctors.FindAsync(id);
        return d is null ? NotFound() : Ok(d);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DoctorDto dto)
    {
        var d = await _db.Doctors.FindAsync(id);
        if (d is null) return NotFound();
        d.FullName = dto.FullName.Trim();
        d.LicenseNo = string.IsNullOrWhiteSpace(dto.LicenseNo) ? null : dto.LicenseNo.Trim();
        d.Email = dto.Email;
        d.Phone = dto.Phone;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var d = await _db.Doctors.FindAsync(id);
        if (d is null) return NotFound();

        _db.Doctors.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

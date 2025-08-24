using System.Text;
using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using MedSys.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly AppDb _db;
    private readonly IRepositoryFactory _factory;

    public PatientsController(AppDb db, IRepositoryFactory factory)
    {
        _db = db;
        _factory = factory;
    }

    // Helper: normaliziraj BirthDate na UTC date (za timestamptz kolonu)
    private static DateTime ToUtcDate(DateTime d)
    {
        if (d.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
        return d.ToUniversalTime().Date;
    }

    // GET /api/patients?lastName=&oib=
    [HttpGet]
    public async Task<ActionResult<List<PatientSummaryDto>>> Search([FromQuery] string? lastName, [FromQuery] string? oib)
    {
        var q = _db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(lastName))
            q = q.Where(p => EF.Functions.ILike(p.LastName, lastName + "%"));
        if (!string.IsNullOrWhiteSpace(oib))
            q = q.Where(p => p.OIB == oib);

        var list = await q.Take(100)
            .Select(p => new PatientSummaryDto(
                p.Id, p.FirstName, p.LastName, p.OIB, p.BirthDate, p.Sex, p.PatientNumber
            ))
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/patients/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> Get(Guid id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p == null) return NotFound();

        var patient = new PatientSummaryDto(p.Id, p.FirstName, p.LastName, p.OIB, p.BirthDate, p.Sex, p.PatientNumber);

        var history = p.MedicalHistory
            .OrderByDescending(x => x.StartDate)
            .Select(x => new MedicalHistoryItemDto(x.Id, x.DiseaseName, x.StartDate, x.EndDate))
            .ToList();

        var visits = p.Visits
            .OrderByDescending(v => v.VisitDateTime)
            .Select(v => new VisitReadDto(
                v.Id,
                v.VisitDateTime,
                v.VisitType,
                v.Notes,
                v.Documents
                    .Select(d => new DocumentDto(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.StorageUrl, d.UploadedAt))
                    .ToList(),
                v.Prescriptions
                    .Select(pr => new PrescriptionReadDto(
                        pr.Id,
                        pr.IssuedAt,
                        pr.Notes,
                        pr.Items
                          .Select(i => new PrescriptionItemReadDto(i.Dosage, i.Frequency, i.DurationDays, i.Medication.Name))
                          .ToList()
                    ))
                    .ToList(),
                v.DoctorId,                               // ⬅︎ dodano
                v.Doctor != null ? v.Doctor.FullName : null // ⬅︎ dodano
            ))
            .ToList();

        return Ok(new PatientDetailsDto(patient, history, visits));
    }

    // POST /api/patients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PatientDto dto)
    {
        var p = new Patient
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            OIB = dto.OIB.Trim(),
            BirthDate = ToUtcDate(dto.BirthDate), // fix timestamptz
            Sex = dto.Sex,
            PatientNumber = dto.PatientNumber
        };

        var repo = _factory.Create<Patient>();
        await repo.AddAsync(p);
        await repo.SaveChangesAsync();

        var outDto = new PatientSummaryDto(
            p.Id, p.FirstName, p.LastName, p.OIB, p.BirthDate, p.Sex, p.PatientNumber);

        return CreatedAtAction(nameof(Get), new { id = p.Id }, outDto);
    }

    // PUT /api/patients/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientDto dto)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p == null) return NotFound();

        p.FirstName = dto.FirstName.Trim();
        p.LastName = dto.LastName.Trim();
        p.OIB = dto.OIB.Trim();
        p.BirthDate = ToUtcDate(dto.BirthDate); // fix timestamptz
        p.Sex = dto.Sex;
        p.PatientNumber = dto.PatientNumber;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/patients/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p == null) return NotFound();
        _db.Patients.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/patients/export.csv
    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var pts = await _db.Patients.AsNoTracking().ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Id,FirstName,LastName,OIB,BirthDate,Sex,PatientNumber");
        foreach (var p in pts)
            sb.AppendLine($"{p.Id},{p.FirstName},{p.LastName},{p.OIB},{p.BirthDate:yyyy-MM-dd},{p.Sex},{p.PatientNumber}");
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "patients.csv");
    }
}

using MedSys.Api.Data;
using MedSys.Api.Dtos;
using MedSys.Api.Models;
using MedSys.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

namespace MedSys.Api.Controllers;

[ApiController]
[Authorize]
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

    private static DateTime AsUtcDate(DateTime d)
    {
        if (d.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
        return d.ToUniversalTime().Date;
    }

    // GET /api/patients?lastName=&oib=
    [HttpGet]
    public async Task<ActionResult<List<PatientSummaryDto>>> Search(
        [FromQuery] string? lastName, [FromQuery] string? oib)
    {
        var q = _db.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            q = q.Where(p => EF.Functions.ILike(p.LastName, lastName + "%"));
            // q = q.Where(p => p.LastName.ToLower().StartsWith(lastName.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(oib))
            q = q.Where(p => p.OIB == oib);

        var list = await q
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Take(100)
            .Select(p => new PatientSummaryDto(
                p.Id, p.FirstName, p.LastName, p.OIB, p.BirthDate, p.Sex, p.PatientNumber))
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/patients/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDetailsDto>> Get(Guid id)
    {
        var p = await _db.Patients
            .Include(x => x.MedicalHistory)
            .Include(x => x.Visits).ThenInclude(v => v.Doctor)
            .Include(x => x.Visits).ThenInclude(v => v.Documents)
            .Include(x => x.Visits).ThenInclude(v => v.Prescriptions).ThenInclude(pr => pr.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(x => x.Id == id);

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
                v.DoctorId,
                v.Doctor != null ? v.Doctor.FullName : null
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
            BirthDate = AsUtcDate(dto.BirthDate),
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
        p.BirthDate = AsUtcDate(dto.BirthDate);
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
    public async Task<IActionResult> ExportListCsv([FromQuery] string? lastName, [FromQuery] string? oib)
    {
        var q = _db.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(lastName))
            q = q.Where(p => EF.Functions.ILike(p.LastName, lastName + "%"));
        if (!string.IsNullOrWhiteSpace(oib))
            q = q.Where(p => p.OIB == oib);

        var rows = await q
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new { p.FirstName, p.LastName, p.OIB, p.BirthDate, p.Sex, p.PatientNumber })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("firstName,lastName,oib,birthDate,sex,patientNumber");
        foreach (var r in rows)
            sb.AppendLine(string.Join(",",
                Csv(r.FirstName), Csv(r.LastName), Csv(r.OIB),
                r.BirthDate.ToString("yyyy-MM-dd"),
                Csv(r.Sex), Csv(r.PatientNumber)));

        var bytes = Utf8Bom(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "patients.csv");
    }

    [HttpGet("{id:guid}/export.csv")]
    public async Task<IActionResult> ExportPatientCsv(Guid id)
    {
        var p = await _db.Patients
            .Include(x => x.MedicalHistory)
            .Include(x => x.Visits).ThenInclude(v => v.Doctor)
            .Include(x => x.Visits).ThenInclude(v => v.Documents)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound();

        string Q(string? s) => $"\"{(s ?? string.Empty).Replace("\"", "\"\"")}\"";

        var sb = new StringBuilder();

        sb.AppendLine("Pacijent,OIB,Datum rođenja,Spol,Broj pacijenta");
        sb.AppendLine($"{Q($"{p.FirstName} {p.LastName}")},{Q(p.OIB)},{p.BirthDate:yyyy-MM-dd},{Q(p.Sex)},{Q(p.PatientNumber)}");
        sb.AppendLine();

        sb.AppendLine("Povijest bolesti");
        sb.AppendLine("Naziv,Početak,Kraj");
        foreach (var h in p.MedicalHistory.OrderByDescending(h => h.StartDate))
            sb.AppendLine($"{Q(h.DiseaseName)},{h.StartDate:yyyy-MM-dd},{(h.EndDate.HasValue ? h.EndDate.Value.ToString("yyyy-MM-dd") : "")}");
        sb.AppendLine();

        sb.AppendLine("Pregledi");
        sb.AppendLine("Datum/vrijeme,Tip,Liječnik,Bilješka,Dokumenti");
        foreach (var v in p.Visits.OrderByDescending(v => v.VisitDateTime))
        {
            var doctor = v.Doctor?.FullName ?? "";
            var docs = v.Documents.Any() ? string.Join(" | ", v.Documents.Select(d => d.FileName)) : "";
            sb.AppendLine($"{v.VisitDateTime:yyyy-MM-dd HH:mm},{Q(v.VisitType)},{Q(doctor)},{Q(v.Notes)},{Q(docs)}");
        }
        sb.AppendLine();

        var patientDocs = await _db.Documents
            .Where(d => d.PatientId == id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        if (patientDocs.Any())
        {
            sb.AppendLine("Dokumenti pacijenta");
            sb.AppendLine("Naziv,Veličina (B),Uploadano");
            foreach (var d in patientDocs)
                sb.AppendLine($"{Q(d.FileName)},{d.SizeBytes},{d.UploadedAt:yyyy-MM-dd HH:mm}");
        }

        var bytes = Utf8Bom(sb.ToString());

        var pretty = $"pacijent_{p.FirstName}_{p.LastName}.csv";
        var ascii = MakeCsvName(pretty); 

        Response.Headers.Remove(HeaderNames.ContentDisposition);
        Response.Headers[HeaderNames.ContentDisposition] =
            $"attachment; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(pretty)}";

        return File(bytes, "text/csv; charset=utf-8");
    }


    // ===== helpers =====

    private static string MakeCsvName(string name)
    {
        if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            name += ".csv";

        var norm = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(norm.Length);
        foreach (var ch in norm)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        var cleaned = Regex.Replace(sb.ToString(), @"[^A-Za-z0-9\.\-_]+", "_");
        cleaned = Regex.Replace(cleaned, "_{2,}", "_").Trim('_');
        return cleaned.ToLowerInvariant();
    }

    private static string Csv(string? s) =>
        s is null ? "" : $"\"{s.Replace("\"", "\"\"")}\"";

    private static byte[] Utf8Bom(string text) =>
        Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
}

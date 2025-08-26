using System.Globalization;
using MedSys.Api.Data;
using MedSys.Api.Models;
using MedSys.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedSys.Api.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[Route("ui")]
public class UiActionsController : Controller
{
    private readonly AppDb _db;
    private readonly IStorageService? _storage;

    private static readonly HashSet<string> AllowedVisitTypes = new(StringComparer.OrdinalIgnoreCase)
    { "GP","KRV","X-RAY","CT","MR","ULTRA","EKG","ECHO","EYE","DERM","DENTA","MAMMO","NEURO" };

    public UiActionsController(AppDb db, IServiceProvider sp)
    {
        _db = db;
        _storage = sp.GetService<IStorageService>();
    }

    
    [HttpPost("patients/{id:guid}/medicalhistory")]
    public async Task<IActionResult> AddMedicalHistory(
        Guid id,
        [FromForm] string diseaseName,
        [FromForm] DateTime startDate,
        [FromForm] DateTime? endDate)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        if (string.IsNullOrWhiteSpace(diseaseName))
            return Redirect($"/patient.html?id={id}&msg=Unesite+naziv+bolesti.");

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
            return Redirect($"/patient.html?id={id}&msg=Kraj+ne+smije+biti+prije+početka.");

        var mh = new MedicalHistory
        {
            PatientId = id,
            DiseaseName = diseaseName.Trim(),
            StartDate = startDate.Date,
            EndDate = endDate?.Date
        };
        _db.MedicalHistory.Add(mh);
        await _db.SaveChangesAsync();
        return Redirect($"/patient.html?id={id}&msg=Povijest+bolesti+spremna.");
    }

    [HttpPost("patients/{id:guid}/visits")]
    public async Task<IActionResult> AddVisit(
        Guid id,
        [FromForm] DateTime visitDateTime,
        [FromForm] string visitType,
        [FromForm] string? notes)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        if (!AllowedVisitTypes.Contains(visitType))
            return Redirect($"/patient.html?id={id}&msg=Neispravan+tip+pregleda.");

        var local = DateTime.SpecifyKind(visitDateTime, DateTimeKind.Local);
        var dto = new DateTimeOffset(local);

        var v = new Visit
        {
            PatientId = id,
            VisitDateTime = dto,
            VisitType = visitType,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim()
        };

        _db.Visits.Add(v);
        await _db.SaveChangesAsync();
        return Redirect($"/patient.html?id={id}&msg=Pregled+spremnjen.");
    }

    [HttpPost("visits/{visitId:guid}/documents")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadDocument(Guid visitId, IFormFile file)
    {
        var visit = await _db.Visits.FindAsync(visitId);
        if (visit == null) return NotFound();

        if (file == null || file.Length == 0)
            return Redirect($"/patient.html?id={visit.PatientId}&msg=Odaberite+datoteku.");

        string url;

        if (_storage != null)
        {

            var path = $"visits/{visitId}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            using var s = file.OpenReadStream();
            url = await _storage.UploadAsync(s, file.ContentType ?? "application/octet-stream", path);
        }
        else
        {

            url = $"https://storage.example/visits/{visitId}/{Path.GetFileName(file.FileName)}";
        }

        var doc = new Document
        {
            VisitId = visitId,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StorageUrl = url
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return Redirect($"/patient.html?id={visit.PatientId}&msg=Dokument+uploadan.");
    }


    [HttpPost("patients/{id:guid}/prescriptions/items")]
    public async Task<IActionResult> AddPrescriptionItem(
        Guid id,
        [FromForm] Guid visitId,
        [FromForm] string medicationName,
        [FromForm] string dosage,
        [FromForm] string frequency,
        [FromForm] int? durationDays)
    {
        var visit = await _db.Visits.FindAsync(visitId);
        if (visit == null || visit.PatientId != id) return NotFound();

        if (string.IsNullOrWhiteSpace(medicationName))
            return Redirect($"/patient.html?id={id}&msg=Unesite+naziv+lijeka.");

        // Upsert lijeka po nazivu
        var med = await _db.Medications.FirstOrDefaultAsync(m =>
            m.Name.ToLower() == medicationName.Trim().ToLower());
        if (med == null)
        {
            med = new Medication { Name = medicationName.Trim() };
            _db.Medications.Add(med);
            await _db.SaveChangesAsync();
        }


        await _db.Entry(visit).Collection(v => v.Prescriptions).LoadAsync();
        var pr = visit.Prescriptions.OrderByDescending(x => x.IssuedAt).FirstOrDefault();
        if (pr == null)
        {
            pr = new Prescription { VisitId = visitId, Notes = null };
            _db.Prescriptions.Add(pr);
            await _db.SaveChangesAsync();
        }

        var item = new PrescriptionItem
        {
            PrescriptionId = pr.Id,
            MedicationId = med.Id,
            Dosage = dosage.Trim(),
            Frequency = frequency.Trim(),
            DurationDays = durationDays
        };
        _db.PrescriptionItems.Add(item);
        await _db.SaveChangesAsync();

        return Redirect($"/patient.html?id={id}&msg=Lijek+dodan+na+recept.");
    }
}

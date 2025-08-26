namespace MedSys.Api.Dtos;

public record PrescriptionItemCreateDto(
    Guid? MedicationId,
    string? MedicationName, 
    string Dosage,
    string Frequency,
    int DurationDays
);

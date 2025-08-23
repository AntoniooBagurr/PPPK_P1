namespace MedSys.Api.Dtos;

public record PrescriptionItemReadDto(
    string Dosage,
    string Frequency,
    int? DurationDays,
    string Medication
);

namespace MedSys.Api.Dtos;

public record VisitReadDto(
    Guid Id,
    DateTimeOffset VisitDateTime,
    string VisitType,
    string? Notes,
    List<DocumentDto> Documents,
    List<PrescriptionReadDto> Prescriptions,
     Guid? DoctorId,          
    string? DoctorName
);

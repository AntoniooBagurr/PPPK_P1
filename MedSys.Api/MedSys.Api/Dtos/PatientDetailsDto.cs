namespace MedSys.Api.Dtos;

public record PatientDetailsDto(
    PatientSummaryDto Patient,
    List<MedicalHistoryItemDto> MedicalHistory,
    List<VisitReadDto> Visits
);

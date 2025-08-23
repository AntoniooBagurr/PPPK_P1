namespace MedSys.Api.Dtos;

public record MedicalHistoryItemDto(
    Guid Id,
    string DiseaseName,
    DateTime StartDate,
    DateTime? EndDate
);

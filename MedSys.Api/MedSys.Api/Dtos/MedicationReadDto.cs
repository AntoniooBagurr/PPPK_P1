namespace MedSys.Api.Dtos;

public record MedicationReadDto(Guid Id, string Name, string? AtcCode);

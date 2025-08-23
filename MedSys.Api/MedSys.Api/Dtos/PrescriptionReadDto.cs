namespace MedSys.Api.Dtos;

public record PrescriptionReadDto(
    Guid Id,
    DateTimeOffset IssuedAt,
    string? Notes,
    List<PrescriptionItemReadDto> Items
);

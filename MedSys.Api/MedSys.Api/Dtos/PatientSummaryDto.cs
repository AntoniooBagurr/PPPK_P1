namespace MedSys.Api.Dtos;

public record PatientSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string OIB,
    DateTime BirthDate,
    string Sex,
    string? PatientNumber
);

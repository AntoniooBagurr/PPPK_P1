namespace MedSys.Api.Dtos;

public record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageUrl,
    DateTimeOffset UploadedAt
);

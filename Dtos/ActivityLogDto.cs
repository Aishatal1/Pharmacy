namespace Pharmacy.Dtos;

public record ActivityLogDto(
    int Id,
    string Action,
    string TableName,
    int? RecordId,
    string? Details,
    DateTime Timestamp,
    string UserName
);
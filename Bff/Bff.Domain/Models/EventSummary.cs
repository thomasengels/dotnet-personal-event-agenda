namespace Bff.Domain.Models;

public sealed record EventSummary(
    Guid Id,
    string Name,
    string? Description,
    string Street,
    string City,
    string PostalCode,
    string Country,
    DateTime StartDate,
    DateTime EndDate);

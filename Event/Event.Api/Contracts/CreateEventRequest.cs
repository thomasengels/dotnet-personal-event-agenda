namespace Event.Api.Contracts;

public sealed record CreateEventRequest(
    string Name,
    string? Description,
    string Street,
    string City,
    string PostalCode,
    string Country,
    DateTime StartDate,
    DateTime EndDate);

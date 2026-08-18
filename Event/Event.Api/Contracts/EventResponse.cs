namespace Event.Api.Contracts;

public sealed record EventResponse(
    Guid Id,
    string Name,
    string? Description,
    string Street,
    string City,
    string PostalCode,
    string Country,
    DateTime StartDate,
    DateTime EndDate)
{
    public static EventResponse FromDomain(DomainEvent @event) => new(
        @event.Id,
        @event.Name,
        @event.Description,
        @event.Location.Street,
        @event.Location.City,
        @event.Location.PostalCode,
        @event.Location.Country,
        @event.StartDate,
        @event.EndDate);
}

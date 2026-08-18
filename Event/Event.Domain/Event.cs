namespace Event.Domain;

public sealed class Event
{
    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public Address Location { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private Event(Guid id, string name, string? description, Address location, DateTime startDate, DateTime endDate)
    {
        Id = id;
        Name = name;
        Description = description;
        Location = location;
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Event CreateNew(string name, string? description, Address location, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (description is { Length: > 255 })
            throw new ArgumentException("Description must be 255 characters or fewer.", nameof(description));
        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));

        return new Event(Guid.NewGuid(), name, description, location, startDate, endDate);
    }
}

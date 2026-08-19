namespace Agenda.Domain.Models;

public sealed class AgendaItem
{
    public Guid Id { get; }
    public int UserId { get; }
    public Guid EventId { get; }
    public DateTime CreatedUtc { get; }

    private AgendaItem(Guid id, int userId, Guid eventId, DateTime createdUtc)
    {
        Id = id;
        UserId = userId;
        EventId = eventId;
        CreatedUtc = createdUtc;
    }

    public static AgendaItem CreateNew(int userId, Guid eventId, DateTime createdUtc)
    {
        if (userId <= 0)
            throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId is required.", nameof(eventId));

        return new AgendaItem(Guid.NewGuid(), userId, eventId, createdUtc);
    }

    public static AgendaItem Reconstitute(Guid id, int userId, Guid eventId, DateTime createdUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));
        if (userId <= 0)
            throw new ArgumentException("UserId must be greater than zero.", nameof(userId));
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId is required.", nameof(eventId));

        return new AgendaItem(id, userId, eventId, createdUtc);
    }
}

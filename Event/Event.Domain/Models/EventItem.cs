namespace Event.Domain.Models;

public sealed record EventItem(int Id, string Name, DateTime ScheduledAtUtc);
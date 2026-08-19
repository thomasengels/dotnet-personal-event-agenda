namespace Agenda.Infrastructure.Entities;

public sealed class AgendaItemEntity
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public Guid EventId { get; set; }
    public DateTime CreatedUtc { get; set; }
}

using Agenda.Domain.Models;
using Agenda.Domain.Ports;
using Agenda.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Infrastructure;

public sealed class AgendaRepository(AgendaDbContext dbContext) : IAgendaRepository
{
    public async Task<IReadOnlyList<AgendaItem>> GetByUserIdAsync(int userId, CancellationToken ct)
    {
        var entities = await dbContext.AgendaItems
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedUtc)
            .ToListAsync(ct);

        return [.. entities.Select(ToDomain)];
    }

    public async Task<AddAgendaItemResult> AddEventAsync(AgendaItem agendaItem, CancellationToken ct)
    {
        var existing = await FindExistingAsync(agendaItem.UserId, agendaItem.EventId, ct);
        if (existing is not null)
            return new AddAgendaItemResult(ToDomain(existing), Created: false);

        dbContext.AgendaItems.Add(ToEntity(agendaItem));

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return new AddAgendaItemResult(agendaItem, Created: true);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            existing = await FindExistingAsync(agendaItem.UserId, agendaItem.EventId, ct);
            if (existing is not null)
                return new AddAgendaItemResult(ToDomain(existing), Created: false);

            throw;
        }
    }

    private Task<AgendaItemEntity?> FindExistingAsync(int userId, Guid eventId, CancellationToken ct) =>
        dbContext.AgendaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EventId == eventId, ct);

    private static AgendaItem ToDomain(AgendaItemEntity entity) =>
        AgendaItem.Reconstitute(entity.Id, entity.UserId, entity.EventId, entity.CreatedUtc);

    private static AgendaItemEntity ToEntity(AgendaItem agendaItem) => new()
    {
        Id = agendaItem.Id,
        UserId = agendaItem.UserId,
        EventId = agendaItem.EventId,
        CreatedUtc = agendaItem.CreatedUtc,
    };
}

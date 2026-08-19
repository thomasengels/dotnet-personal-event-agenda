using Agenda.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agenda.Infrastructure;

public sealed class AgendaItemEntityConfiguration : IEntityTypeConfiguration<AgendaItemEntity>
{
    public void Configure(EntityTypeBuilder<AgendaItemEntity> builder)
    {
        builder.ToTable("agenda_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.EventId).IsRequired();
        builder.Property(e => e.CreatedUtc).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
    }
}

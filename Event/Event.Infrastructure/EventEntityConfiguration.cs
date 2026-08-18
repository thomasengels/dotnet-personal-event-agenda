using Event.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure;

public sealed class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(255);

        builder.OwnsOne(e => e.Location, location =>
        {
            location.Property(a => a.Street).HasColumnName("Street").IsRequired();
            location.Property(a => a.City).HasColumnName("City").IsRequired();
            location.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired();
            location.Property(a => a.Country).HasColumnName("Country").IsRequired();
        });

        builder.Navigation(e => e.Location).IsRequired();
    }
}

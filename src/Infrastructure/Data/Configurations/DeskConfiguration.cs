using Domain.Desks;
using Domain.Locations;
using Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class DeskConfiguration : EntityConfiguration<Desk>
{
    public override void Configure(EntityTypeBuilder<Desk> builder)
    {
        builder.ToTable("Desks");

        builder.HasIndex(d => d.Name)
            .IsUnique();
        builder.Property(d => d.Name)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(255);

        builder.HasOne<Location>(d => d.Location)
            .WithMany(l => l.Desks)
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Reservation>(d => d.Reservations)
            .WithOne(r => r.Desk)
            .HasForeignKey(r => r.DeskId)
            .OnDelete(DeleteBehavior.NoAction);
        
        base.Configure(builder);
    }
}
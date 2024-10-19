using Domain.Desks;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class LocationConfiguration : EntityConfiguration<Location>
{
    public override void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.Property(l => l.Name)
            .IsRequired();

        builder.OwnsOne<Address>(l => l.Address, a =>
        {
            a.Property(ad => ad.Street)
                .HasMaxLength(120)
                .IsRequired();

            a.Property(ad => ad.BuildingNumber)
                .HasMaxLength(16)
                .IsRequired();
            
            a.Property(ad => ad.City)
                .HasMaxLength(100)
                .IsRequired();

            a.Property(ad => ad.PostalCode)
                .HasMaxLength(16)
                .IsRequired();
        });

        builder.HasMany<Desk>(l => l.Desks)
            .WithOne(d => d.Location)
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.NoAction);
        
        base.Configure(builder);
    }
}
using Domain.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

public class ReservationConfiguration : EntityConfiguration<Reservation>
{
    public override void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.Property(r => r.StartDate)
            .IsRequired();

        builder.Property(r => r.EndDate)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(r => r.Desk)
            .WithMany(d => d.Reservations)
            .HasForeignKey(r => r.DeskId)
            .OnDelete(DeleteBehavior.NoAction);
        
        base.Configure(builder);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.RejectReason).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DiningTable)
            .WithMany()
            .HasForeignKey(x => x.DiningTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProcessedBy)
            .WithMany()
            .HasForeignKey(x => x.ProcessedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledBy)
            .WithMany()
            .HasForeignKey(x => x.CancelledById)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtrirani unique indeks: isti gost ne moze imati dvije aktivne rezervacije
        // (Status 0 = Pending, 1 = Confirmed) za isti termin; zavrsene/otkazane/odbijene ne blokiraju novi termin.
        builder.HasIndex(x => new { x.UserId, x.ReservationAt })
            .IsUnique()
            .HasFilter("[Status] IN (0, 1)");
    }
}

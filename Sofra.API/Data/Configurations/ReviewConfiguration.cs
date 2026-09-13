using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.Property(x => x.Reply).HasMaxLength(1000);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MenuItem)
            .WithMany()
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Waiter)
            .WithMany()
            .HasForeignKey(x => x.WaiterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RepliedBy)
            .WithMany()
            .HasForeignKey(x => x.RepliedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Kompozitni unique: jedan korisnik moze ostaviti jednu recenziju po (narudzba, jelo),
        // a MenuItemId = null (ocjena usluge) je po narudzbi/korisniku takodjer jedinstven.
        builder.HasIndex(x => new { x.OrderId, x.UserId, x.MenuItemId }).IsUnique();
    }
}

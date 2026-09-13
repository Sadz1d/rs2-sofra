using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class MenuItemStatsConfiguration : IEntityTypeConfiguration<MenuItemStats>
{
    public void Configure(EntityTypeBuilder<MenuItemStats> builder)
    {
        builder.Property(x => x.PopularityScore).HasPrecision(5, 4);
        builder.Property(x => x.AvgRatingNormalized).HasPrecision(5, 4);

        builder.HasOne(x => x.MenuItem)
            .WithMany()
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MenuItemId).IsUnique();
    }
}

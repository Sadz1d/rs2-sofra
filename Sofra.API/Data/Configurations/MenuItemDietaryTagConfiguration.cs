using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class MenuItemDietaryTagConfiguration : IEntityTypeConfiguration<MenuItemDietaryTag>
{
    public void Configure(EntityTypeBuilder<MenuItemDietaryTag> builder)
    {
        builder.HasKey(x => new { x.MenuItemId, x.DietaryTagId });

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.MenuItemDietaryTags)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (ne Cascade): brisanje oznake koja se koristi mora biti eksplicitno blokirano
        // u servisu s jasnom porukom, ne tiho obrisati vezu sa jelima.
        builder.HasOne(x => x.DietaryTag)
            .WithMany()
            .HasForeignKey(x => x.DietaryTagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

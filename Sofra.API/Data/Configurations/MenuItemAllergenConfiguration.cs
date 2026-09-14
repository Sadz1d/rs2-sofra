using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class MenuItemAllergenConfiguration : IEntityTypeConfiguration<MenuItemAllergen>
{
    public void Configure(EntityTypeBuilder<MenuItemAllergen> builder)
    {
        builder.HasKey(x => new { x.MenuItemId, x.AllergenId });

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.MenuItemAllergens)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (ne Cascade): brisanje alergena koji se koristi mora biti eksplicitno blokirano
        // u servisu s jasnom porukom, ne tiho obrisati vezu sa jelima.
        builder.HasOne(x => x.Allergen)
            .WithMany()
            .HasForeignKey(x => x.AllergenId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

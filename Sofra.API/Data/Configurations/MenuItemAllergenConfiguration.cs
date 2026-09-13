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

        builder.HasOne(x => x.Allergen)
            .WithMany()
            .HasForeignKey(x => x.AllergenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

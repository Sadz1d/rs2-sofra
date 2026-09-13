using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class MenuItemPairConfiguration : IEntityTypeConfiguration<MenuItemPair>
{
    public void Configure(EntityTypeBuilder<MenuItemPair> builder)
    {
        builder.HasOne(x => x.MenuItemA)
            .WithMany()
            .HasForeignKey(x => x.MenuItemAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MenuItemB)
            .WithMany()
            .HasForeignKey(x => x.MenuItemBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MenuItemAId, x.MenuItemBId }).IsUnique();
    }
}

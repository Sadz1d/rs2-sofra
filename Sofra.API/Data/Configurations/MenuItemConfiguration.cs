using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.AvgRating).HasPrecision(3, 2);

        builder.HasOne(x => x.MenuCategory)
            .WithMany()
            .HasForeignKey(x => x.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete: obrisana jela se automatski izostavljaju iz svih upita.
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

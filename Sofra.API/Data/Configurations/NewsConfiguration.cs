using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Text).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class ProcessedStripeEventConfiguration : IEntityTypeConfiguration<ProcessedStripeEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedStripeEvent> builder)
    {
        builder.Property(x => x.StripeEventId).HasMaxLength(100);
        builder.HasKey(x => x.StripeEventId);
    }
}

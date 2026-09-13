using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sofra.API.Entities;

namespace Sofra.API.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(10, 2);
        builder.Property(x => x.RefundedAmount).HasPrecision(10, 2);
        builder.Property(x => x.StripePaymentIntentId).HasMaxLength(100);
        builder.Property(x => x.StripeRefundId).HasMaxLength(100);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filtrirani unique indeks: gotovinska placanja imaju StripePaymentIntentId = null,
        // a jednokolonski unique indeks u SQL Serveru bez filtera dozvoljava samo jedan NULL.
        builder.HasIndex(x => x.StripePaymentIntentId)
            .IsUnique()
            .HasFilter("[StripePaymentIntentId] IS NOT NULL");
    }
}

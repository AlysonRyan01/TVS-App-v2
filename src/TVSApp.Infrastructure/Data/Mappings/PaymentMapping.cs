using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TVS_App.Domain.Entities;

namespace TVS_App.Infrastructure.Data.Mappings;

public class PaymentMapping : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .IsRequired();

        builder.Property(x => x.ServiceOrderId)
            .HasColumnName("ServiceOrderId")
            .IsRequired();

        builder.HasOne(x => x.ServiceOrder)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

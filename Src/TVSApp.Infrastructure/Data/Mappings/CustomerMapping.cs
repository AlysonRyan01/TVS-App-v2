using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TVS_App.Domain.Entities;

namespace TVS_App.Infrastructure.Data.Mappings;

public class CustomerMapping : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.OwnsOne(c => c.Name, name =>
        {
            name.Property(n => n.CustomerName)
                .HasColumnName("Name")
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(150).IsRequired(false);
            address.Property(a => a.Neighborhood).HasColumnName("Neighborhood").HasMaxLength(100).IsRequired(false);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100).IsRequired(false);
            address.Property(a => a.Number).HasColumnName("Number").HasMaxLength(20).IsRequired(false);
            address.Property(a => a.ZipCode).HasColumnName("ZipCode").HasMaxLength(10).IsRequired(false);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(2).IsRequired(false);
        });

        builder.OwnsOne(c => c.Phone, phone =>
        {
            phone.Property(p => p.CustomerPhone)
                .HasColumnName("Phone")
                .HasMaxLength(20)
                .IsRequired(false);
        });

        builder.OwnsOne(c => c.Phone2, phone2 =>
        {
            phone2.Property(p => p.CustomerPhone)
                .HasColumnName("Phone2")
                .HasMaxLength(20)
                .IsRequired(false);
        });

        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.CustomerEmail)
                .HasColumnName("Email")
                .HasMaxLength(100);
        });
    }
}
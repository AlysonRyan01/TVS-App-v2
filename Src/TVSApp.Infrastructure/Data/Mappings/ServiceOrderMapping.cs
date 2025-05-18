using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TVS_App.Domain.Entities;

namespace TVS_App.Infrastructure.Data.Mappings;

public class ServiceOrderMapping : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.EntryDate).IsRequired();
        builder.Property(x => x.InspectionDate);
        builder.Property(x => x.ResponseDate);
        builder.Property(x => x.PurchasePartDate);
        builder.Property(x => x.RepairDate);
        builder.Property(x => x.DeliveryDate);

        builder.Property(x => x.Enterprise)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ServiceOrderStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RepairStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RepairResult)
            .HasConversion<int?>();

        builder.Property(x => x.SecurityCode)
            .HasColumnName("SecurityCode")
            .HasMaxLength(10);

        builder.Property(so => so.CustomerId)
            .HasColumnName("CustomerId");
            
        builder.HasOne(so => so.Customer)
            .WithMany(c => c.ServiceOrders)
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(x => x.Product, p =>
        {
            p.Property(x => x.Brand)
                .HasColumnName("Product_Brand")
                .HasMaxLength(100);

            p.Property(x => x.Model)
                .HasColumnName("Product_Model")
                .HasMaxLength(100);

            p.Property(x => x.SerialNumber)
                .HasColumnName("Product_SerialNumber")
                .HasMaxLength(200);

            p.Property(x => x.Defect)
                .HasColumnName("Product_Defect")
                .HasMaxLength(300);

            p.Property(x => x.Accessories)
                .HasColumnName("Product_Accessories")
                .HasMaxLength(300);

            p.Property(x => x.Type)
                .HasColumnName("Product_Type");
                
            p.Property(x => x.Location)
                .HasColumnName("Product_Location")
                .HasMaxLength(100);
        });

        builder.OwnsOne(x => x.Solution, s =>
        {
            s.Property(x => x.ServiceOrderSolution)
                .HasColumnName("Solution")
                .HasMaxLength(500);
        });

        builder.OwnsOne(x => x.Guarantee, g =>
        {
            g.Property(x => x.ServiceOrderGuarantee)
                .HasColumnName("Guarantee")
                .HasMaxLength(300);
        });

        builder.OwnsOne(x => x.PartCost, pc =>
        {
            pc.Property(x => x.ServiceOrderPartCost)
                .HasColumnName("PartCost")
                .HasPrecision(18, 2);
        });

        builder.OwnsOne(x => x.LaborCost, lc =>
        {
            lc.Property(x => x.ServiceOrderLaborCost)
                .HasColumnName("LaborCost")
                .HasPrecision(18, 2);
        });

        builder.Property(x => x.EstimateMessage)
            .HasColumnName("EstimateMessage")
            .IsRequired(false);
    }
}
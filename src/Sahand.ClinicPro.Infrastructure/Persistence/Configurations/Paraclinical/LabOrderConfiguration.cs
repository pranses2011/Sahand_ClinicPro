using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول LabOrders — درخواست آزمایش - از پزشک برای بیمار</summary>
public class LabOrderConfiguration : IEntityTypeConfiguration<LabOrder>
{
    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("LabOrders", t => t.HasComment("درخواست آزمایش - از پزشک برای بیمار"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('LAB-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqLabOrderNumber AS varchar(10)), 6))").HasComment("شماره درخواست - یکتا");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت درخواست");

        builder.HasIndex(x => new { x.OrderNumber }).IsUnique().HasDatabaseName("IX_LabOrder_OrderNumber").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_LabOrder_PatientId");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول ImagingOrders — درخواست تصویربرداری - سونو یا رادیو یا CT یا MRI</summary>
public class ImagingOrderConfiguration : IEntityTypeConfiguration<ImagingOrder>
{
    public void Configure(EntityTypeBuilder<ImagingOrder> builder)
    {
        builder.ToTable("ImagingOrders", t => t.HasComment("درخواست تصویربرداری - سونو یا رادیو یا CT یا MRI"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('IMG-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqImagingOrderNumber AS varchar(10)), 6))").HasComment("شماره درخواست - یکتا");
        builder.Property(x => x.Modality).HasConversion<byte>().HasComment("نوع تصویربرداری");
        builder.Property(x => x.BodyPart).HasMaxLength(200).HasComment("ناحیه بدن");
        builder.Property(x => x.Priority).HasConversion<byte>().HasComment("اولویت");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

        builder.HasIndex(x => new { x.OrderNumber }).IsUnique().HasDatabaseName("IX_ImagingOrder_OrderNumber").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_ImagingOrder_PatientId");
    }
}

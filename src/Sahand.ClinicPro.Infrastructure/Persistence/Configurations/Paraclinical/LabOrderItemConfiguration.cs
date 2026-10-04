using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول LabOrderItems — قلم آزمایش - نتیجه یک تست خاص</summary>
public class LabOrderItemConfiguration : IEntityTypeConfiguration<LabOrderItem>
{
    public void Configure(EntityTypeBuilder<LabOrderItem> builder)
    {
        builder.ToTable("LabOrderItems", t => t.HasComment("قلم آزمایش - نتیجه یک تست خاص"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Result).HasMaxLength(200).HasComment("نتیجه متنی");
        builder.Property(x => x.ResultNumeric).HasPrecision(18, 4).HasComment("نتیجه عددی");
        builder.Property(x => x.NormalRange).HasMaxLength(100).HasComment("حد طبیعی");
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد");
        builder.Property(x => x.AbnormalFlag).HasMaxLength(5).HasComment("پرچم غیرطبیعی مثل H یا L");
        builder.Property(x => x.Comment).HasMaxLength(200).HasComment("توضیح آزمایشگاه");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت قلم");

    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Personnel;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول EmployeeLeaves — مرخصی پرسنل - درخواست و تأیید</summary>
public class EmployeeLeaveConfiguration : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> builder)
    {
        builder.ToTable("EmployeeLeaves", t => t.HasComment("مرخصی پرسنل - درخواست و تأیید"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LeaveType).HasConversion<byte>().HasComment("نوع مرخصی");
        builder.Property(x => x.Days).HasPrecision(4, 1).HasComment("تعداد روز");
        builder.Property(x => x.Reason).HasMaxLength(200).HasComment("دلیل");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت");

    }
}

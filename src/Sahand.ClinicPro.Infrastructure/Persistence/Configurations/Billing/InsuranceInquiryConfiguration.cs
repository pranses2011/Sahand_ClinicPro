using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول InsuranceInquiries — استعلام بیمه - پاسخ آنلاین بیمه در لحظه پذیرش</summary>
public class InsuranceInquiryConfiguration : IEntityTypeConfiguration<InsuranceInquiry>
{
    public void Configure(EntityTypeBuilder<InsuranceInquiry> builder)
    {
        builder.ToTable("InsuranceInquiries", t => t.HasComment("استعلام بیمه - پاسخ آنلاین بیمه در لحظه پذیرش"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApprovalCode).HasMaxLength(50).HasComment("کد تأیید دریافتی");

    }
}

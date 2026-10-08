using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Scheduling;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Receptions — پذیرش - ورود بیمار به جریان ویزیت در روز مراجعه</summary>
public class ReceptionConfiguration : IEntityTypeConfiguration<Reception>
{
    public void Configure(EntityTypeBuilder<Reception> builder)
    {
        builder.ToTable("Receptions", t => t.HasComment("پذیرش - ورود بیمار به جریان ویزیت در روز مراجعه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceptionNumber).HasMaxLength(20).HasDefaultValueSql("CONCAT('RCP-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqReceptionNumber AS varchar(10)), 6))").HasComment("شماره پذیرش - فرمت RCP-XXXXXXXX");
        builder.Property(x => x.ReceptionDateShamsi).HasMaxLength(200).HasComment("تاریخ شمسی پذیرش");
        builder.Property(x => x.InsuranceApprovalCode).HasMaxLength(50).HasComment("کد تأیید بیمه");
        builder.Property(x => x.SupplementaryInsuranceCode).HasMaxLength(20).HasComment("کد بیمه تکمیلی");
        builder.Property(x => x.ReferralCode).HasMaxLength(50).HasComment("کد ارجاع");
        builder.Property(x => x.ReferringDoctor).HasMaxLength(100).HasComment("نام پزشک ارجاع‌دهنده");
        // چرخه ۲۵ — هم‌راستایی مدل با مهاجرت دست‌نویس Cycle24ReceptionServices:
        // ستون دیتابیسی nvarchar(200) است؛ بدون این سقف، EF به‌صورت کنوانسیون
        // nvarchar(max) می‌سازد و مهاجرت‌های آتیِ تولیدشده بی‌دلیل ALTER می‌شدند
        builder.Property(x => x.ServiceName).HasMaxLength(200).HasComment("نام خدمت پذیرش‌شده — نمایش پایدار در فهرست‌ها (چرخه ۲۴)");
        builder.Property(x => x.Status).HasConversion<byte>().HasComment("وضعیت پذیرش");

        builder.HasIndex(x => new { x.ReceptionNumber }).IsUnique().HasDatabaseName("IX_Reception_ReceptionNumber").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.PatientId }).HasDatabaseName("IX_Reception_PatientId");
    }
}

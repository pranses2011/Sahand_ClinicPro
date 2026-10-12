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

        // ═══ چرخهٔ ۵۹ (P-02 نقشه راه ai-review) — ایندکس‌های ترکیبی صف پذیرش ═══
        // داغ‌ترین پرس‌وجوی برنامه: «پذیرش‌های امروز» (صف + آمار صف) روی بازهٔ ReceptionDate
        // — تا این چرخه هیچ ایندکسی روی ReceptionDate نبود و هر بازکردن صفحهٔ صف = Full Scan.
        builder.HasIndex(x => new { x.ReceptionDate, x.Status })
            .HasDatabaseName("IX_Reception_ReceptionDate_Status");
        // میزکار پزشک/فهرست پذیرش هر پزشک در یک روز — ترکیب به‌جای دو جست‌وجوی مجزا
        builder.HasIndex(x => new { x.DoctorId, x.ReceptionDate })
            .HasDatabaseName("IX_Reception_DoctorId_ReceptionDate");

        // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — اتاق انجام خدمت در پذیرش خدمتی ═══
        // «اتاق معاینه برای پزشک در برنامهٔ کاری پزشک مشخص می‌شود ولی برای
        // خدمات درمانی نمی‌شود معین کرد» — ستون RoomId به جدول پذیرش‌ها
        // اضافه شد؛ رابطهٔ ناوبری بدون مجموعهٔ سمت اتاق (ناوبری فقط از پذیرش
        // به اتاق خوانده می‌شود) و حذف NoAction تا حذف اتاق هرگز پذیرش‌های
        // تاریخی را نکشد (اتاق‌ها فقط نرم-غیرفعال می‌شوند).
        builder.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Property(x => x.RoomId).HasComment("اتاق انجام خدمت — پذیرش خدمتی چرخهٔ ۳۹ (ویزیت پزشک: اتاق از برنامهٔ کاری خود پزشک)");
        // ایندکس پرس‌وجو بر اساس اتاق (بار اتاق‌ها در طول روز) — نام کانونشنی EF
        builder.HasIndex(x => x.RoomId);
    }
}

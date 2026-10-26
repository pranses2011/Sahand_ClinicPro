using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Patients — بیمار — مرکزی‌ترین موجودیت سیستم که تمام ماژول‌ها به آن وابسته هستند</summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", t => t.HasComment("بیمار — مرکزی‌ترین موجودیت سیستم که تمام ماژول‌ها به آن وابسته هستند"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicalRecordNo).HasMaxLength(200).IsRequired().HasDefaultValueSql("CONCAT('MRN-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqPatientMrn AS varchar(10)), 6))").HasComment("شماره پرونده پزشکی - تولید خودکار با فرمت MRN-XXXXXX");
        builder.Property(x => x.NationalCode).HasMaxLength(10).HasComment("کد ملی ۱۰ رقمی - یکتا و با الگوریتم رقم کنترلی اعتبارسنجی می‌شود؛ برای بیمار ناشناس تهی است");
        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired().HasComment("نام بیمار - حداکثر ۵۰ کاراکتر");
        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired().HasComment("نام خانوادگی بیمار - ایندکس‌شده برای جستجو");
        builder.Property(x => x.FatherName).HasMaxLength(50).HasComment("نام پدر بیمار");
        builder.Property(x => x.Gender).HasConversion<byte>().HasComment("جنسیت بیمار");
        builder.Property(x => x.BirthDateShamsi).HasMaxLength(200).HasComment("تاریخ تولد شمسی موازی - فرمت 1358/04/15");
        builder.Property(x => x.BloodType).HasMaxLength(5).HasComment("گروه خونی مثل A+ یا O-");
        builder.Property(x => x.MaritalStatus).HasConversion<byte>().HasComment("وضعیت تأهل");

        // برچسب‌های بیمار CSV — چرخهٔ ۶۹ (فاز ۳ / F-18 «برچسب بیمار (VIP/مزمن)»)
        builder.Property(x => x.Tags).HasMaxLength(200).HasComment("برچسب‌های بیمار CSV — چرخهٔ ۶۹ (F-18)");
        builder.Property(x => x.Mobile).HasMaxLength(11).IsRequired().HasComment("شماره موبایل ۱۱ رقمی - الگوی 09XXXXXXXXX - ایندکس‌شده");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن ثابت");
        builder.Property(x => x.Email).HasMaxLength(100).HasComment("ایمیل بیمار");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("آدرس - با AES-256 رمزنگاری می‌شود");
        builder.Property(x => x.City).HasMaxLength(50).HasComment("شهر");
        builder.Property(x => x.Province).HasMaxLength(50).HasComment("استان");
        builder.Property(x => x.PostalCode).HasMaxLength(10).HasComment("کد پستی ۱۰ رقمی");
        builder.Property(x => x.EmergencyContactName).HasMaxLength(200).HasComment("نام تماس اضطراری");
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(11).HasComment("تلفن تماس اضطراری");
        builder.Property(x => x.EmergencyContactRelation).HasMaxLength(50).HasComment("نسبت تماس اضطراری با بیمار");
        builder.Property(x => x.InsuranceNumber).HasMaxLength(20).HasComment("شماره دفترچه بیمه");
        builder.Property(x => x.InsuranceType).HasMaxLength(50).HasComment("نوع بیمه - اصلی یا تبعی");
        builder.Property(x => x.SepasPatientId).HasMaxLength(50).HasComment("شناسه بیمار در سامانه سپاس");
        builder.Property(x => x.Occupation).HasMaxLength(100).HasComment("شغل بیمار");
        builder.Property(x => x.Education).HasConversion<byte>().HasComment("سطح تحصیلات");
        builder.Property(x => x.Nationality).HasMaxLength(50).HasComment("ملیت - پیش‌فرض ایرانی");
        builder.Property(x => x.PatientType).HasConversion<byte>().HasComment("نوع بیمار - عادی یا ویژه یا کارمند");
        builder.Property(x => x.ProfilePicture).HasMaxLength(500).HasComment("مسیر عکس پروفایل");

        builder.HasIndex(x => new { x.NationalCode }).IsUnique().HasDatabaseName("IX_Patient_NationalCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.MedicalRecordNo }).IsUnique().HasDatabaseName("IX_Patient_MedicalRecordNo").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.Mobile }).HasDatabaseName("IX_Patient_Mobile");
        builder.HasIndex(x => new { x.LastName }).HasDatabaseName("IX_Patient_LastName");
    }
}

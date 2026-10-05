using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Employees — کارمند درمانگاه - پرسنل اداری و درمانی</summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", t => t.HasComment("کارمند درمانگاه - پرسنل اداری و درمانی"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PersonnelCode).HasMaxLength(20).HasComment("کد پرسنلی - یکتا");
        builder.Property(x => x.NationalCode).HasMaxLength(10).HasComment("کد ملی - یکتا");
        builder.Property(x => x.FirstName).HasMaxLength(50).IsRequired().HasComment("نام");
        builder.Property(x => x.LastName).HasMaxLength(50).IsRequired().HasComment("نام خانوادگی");
        builder.Property(x => x.FatherName).HasMaxLength(50).HasComment("نام پدر");
        builder.Property(x => x.Gender).HasConversion<byte>().HasComment("جنسیت");
        builder.Property(x => x.MaritalStatus).HasConversion<byte>().HasComment("وضعیت تأهل");
        builder.Property(x => x.Mobile).HasMaxLength(11).HasComment("موبایل");
        builder.Property(x => x.Phone).HasMaxLength(15).HasComment("تلفن ثابت");
        builder.Property(x => x.Email).HasMaxLength(100).HasComment("ایمیل");
        builder.Property(x => x.Address).HasMaxLength(500).HasComment("آدرس");
        builder.Property(x => x.EmploymentType).HasConversion<byte>().HasComment("نوع استخدام");
        builder.Property(x => x.ContractType).HasConversion<byte>().HasComment("نوع قرارداد");
        builder.Property(x => x.BaseSalary).HasPrecision(18, 0).HasComment("حقوق پایه به ریال");
        builder.Property(x => x.CommissionPercent).HasPrecision(5, 2).HasComment("درصد حقوق از سود خدمات انجامی خود پرسنل (نه کل دریافتی) — صفر = فقط حقوق پایه");
        builder.Property(x => x.BankName).HasMaxLength(50).HasComment("نام بانک");
        builder.Property(x => x.BankAccount).HasMaxLength(50).HasComment("شماره حساب بانکی");
        builder.Property(x => x.Sheba).HasMaxLength(26).HasComment("شماره شبا ۲۶ کاراکتری");
        builder.Property(x => x.EducationLevel).HasConversion<byte>().HasComment("سطح تحصیلات");
        builder.Property(x => x.FieldOfStudy).HasMaxLength(100).HasComment("رشته تحصیلی");
        builder.Property(x => x.EmergencyContact).HasMaxLength(200).HasComment("نام تماس اضطراری");
        builder.Property(x => x.EmergencyPhone).HasMaxLength(15).HasComment("تلفن اضطراری");
        builder.Property(x => x.ProfilePicture).HasMaxLength(500).HasComment("مسیر عکس");

        builder.HasIndex(x => new { x.PersonnelCode }).IsUnique().HasDatabaseName("IX_Employee_PersonnelCode").HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.NationalCode }).IsUnique().HasDatabaseName("IX_Employee_NationalCode").HasFilter("[IsDeleted] = 0");
    }
}

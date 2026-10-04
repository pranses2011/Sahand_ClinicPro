using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>پیکربندی EF Core برای جدول Services — خدمت - فهرست خدمات قابل ارائه با قیمت پایه</summary>
public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", t => t.HasComment("خدمت - فهرست خدمات قابل ارائه با قیمت پایه"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceCode).HasMaxLength(20).HasComment("کد خدمت - یکتا");
        builder.Property(x => x.ServiceName).HasMaxLength(50).IsRequired().HasComment("نام خدمت");
        builder.Property(x => x.ServiceNameEn).HasMaxLength(200).HasComment("نام انگلیسی خدمت");
        builder.Property(x => x.ServiceType).HasConversion<byte>().HasComment("نوع خدمت");
        builder.Property(x => x.Unit).HasMaxLength(20).HasComment("واحد سنجش");
        builder.Property(x => x.DefaultPrice).HasPrecision(18, 0).HasComment("قیمت پیش‌فرض");
        builder.Property(x => x.KCoefficient).HasPrecision(10, 2).HasComment("ضریب K");
        builder.Property(x => x.NationalCode).HasMaxLength(10).HasComment("کد ملی خدمت برای بیمه");

        builder.HasIndex(x => new { x.ServiceCode }).IsUnique().HasDatabaseName("IX_Service_ServiceCode").HasFilter("[IsDeleted] = 0");
    }
}

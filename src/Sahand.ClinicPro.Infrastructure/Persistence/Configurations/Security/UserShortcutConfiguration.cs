using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sahand.ClinicPro.Domain.Entities.Security;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF Core برای جدول UserShortcuts — میانبرهای سریع داشبورد کاربر (چرخهٔ ۴۱)
/// </summary>
/// <remarks>
/// ایندکس یکتای UserId «فیلترشده» است (فقط ردیف‌های سالم IsDeleted=0) — همان
/// الگوی یگانه‌سازی جدول‌های نرم‌حذفشونده در این پروژه (IX_Waitlist_Status_Doctor)
/// تا اگر روزی ردیفی نرم‌حذف شد، درج ردیف تازه برای همان کاربر به قید یکتایی
/// برنخورد؛ Upsert کنترلر علاوه بر آن با IgnoreQueryFilters ردیف نرم‌حذف‌شده
/// را بازمی‌گرداند (دو لایهٔ دفاعی مستقل).
/// </remarks>
public class UserShortcutConfiguration : IEntityTypeConfiguration<UserShortcut>
{
    /// <summary>پیکربندی جدول، ستون‌ها و ایندکس یکتا</summary>
    public void Configure(EntityTypeBuilder<UserShortcut> builder)
    {
        builder.ToTable("UserShortcuts", t => t.HasComment("میانبرهای سریع داشبورد کاربر — چرخهٔ ۴۱"));
        builder.HasKey(x => x.Id);

        // شناسهٔ کاربر — هر کاربر حداکثر یک ردیف سالم
        builder.Property(x => x.UserId).HasComment("شناسه کاربر مالک میانبرها — یکتا در ردیف‌های سالم");

        // کلیدهای پین‌شده — آرایهٔ JSON؛ پیش‌فرض «هیچ» تا ردیفِ خالی بی‌معنا نسازیم
        builder.Property(x => x.KeysJson)
            .HasMaxLength(4000)
            .HasDefaultValue("[]")
            .HasComment("کلید صفحات پین‌شدهٔ داشبورد به‌صورت JSON آرایه‌ای");

        // یکتایی به‌ازای کاربر — فقط روی ردیف‌های زنده (نرم‌حذف مستثناست)
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserShortcuts_UserId")
            .HasFilter("[IsDeleted] = 0");
    }
}

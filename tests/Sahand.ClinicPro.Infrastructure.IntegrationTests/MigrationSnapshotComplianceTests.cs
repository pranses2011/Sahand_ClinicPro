using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های سازگاری Snapshot مایگریشن‌ها — چرخهٔ ۵۴ (BUG-10):
/// مایگریشن‌های دست‌نویسِ ثبت‌شده در فهرست طلایی فایل Designer (و مدل هدفِ snapshot) ندارند؛
/// این توسط خودشان با ویژگی‌های [DbContext]/[Migration] پوشش داده شده و
/// گارد کشفِ چرخهٔ ۲۵ (MigrationDiscoveryGuardTests) رابطهٔ EF را می‌سنجد.
/// این‌جا سه گارد تکمیلی: یکتایی شناسه‌ها، ترتیب زمانی، و «فهرست طلایی» —
/// هر مایگریشنِ دست‌نویسِ تازه‌ای که به این لیست اضافه شود باید آگاهانه
/// ثبت شود وگرنه این تست پیش از انتشارش را می‌گیرد.
/// </summary>
public class MigrationSnapshotComplianceTests
{
    /// <summary>
    /// فهرست طلایی مایگریشن‌های دست‌نویس بدون Designer — ۲۰۲۶/۰۷/۲۱ (چرخهٔ ۵۴).
    /// اگر مایگریشن تازه‌ای «عمداً» بدون Designer نوشته شد، نام کلاسش را آگاهانه
    /// به این مجموعه اضافه کنید؛ هر مورد دیگر = خطای فرآیند انتشار.
    /// </summary>
    private static readonly HashSet<string> KnownHandwrittenWithoutSnapshot = new()
    {
        "Cycle21TemplateItemsGuard",
        "Cycle23TemplateDoctorNullable",
        "Cycle24ReceptionServices",
        "Cycle39ReceptionRoom",
        "Cycle39MedicationItemKind",
        "Cycle41UserShortcuts",
        "Cycle42SeedPositionsDepartments",
        "Cycle59PerformanceIndexes", // چرخهٔ ۵۹ — ایندکس‌های ترکیبی صف/فاکتور (P-02)
        "Cycle69AutoReminders",      // چرخهٔ ۶۹ (F-03) — ستون Reminder2Sent نوبت
        "Cycle69LabReferenceRanges", // چرخهٔ ۶۹ (F-28/F-29) — بازه‌های مرجع ساخت‌یافتهٔ آزمایش
        "Cycle69PatientTags",        // چرخهٔ ۶۹ (F-18) — برچسب CSV بیمار
        "AddEmployeeInsuranceFlag",  // گزینهٔ شمول بیمه در پروندهٔ پرسنل
    };

    /// <summary>همهٔ کلاس‌های مایگریشن اسمبلی زیرساخت</summary>
    private static List<Type> MigrationTypes() =>
        typeof(ClinicProDbContext).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(typeof(Migration).IsAssignableFrom)
            .ToList();

    /// <summary>
    /// آیا این کلاس مدل هدف (Designer) دارد؟ — تشخیص با بازتاب:
    /// فایل‌های تولیدی EF متد BuildTargetModel را override می‌کنند؛ دست‌نویس‌ها نه.
    /// </summary>
    private static bool HasTargetModel(Type t) =>
        t.GetMethod("BuildTargetModel",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is not null;

    [Fact]
    public void Migration_Ids_Should_Be_Unique()
    {
        var ids = MigrationTypes()
            .Select(t => t.GetCustomAttribute<MigrationAttribute>()?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        ids.Distinct().Count().Should().Be(ids.Count,
            because: "شناسهٔ تکراری مایگریشن یعنی EF یکی را بی‌صدا نادیده می‌گیرد و اسکیمای سرور ناقص می‌ماند");
    }

    [Fact]
    public void Migration_Ids_Should_Be_Chronologically_Ordered()
    {
        var ids = MigrationTypes()
            .Select(t => t.GetCustomAttribute<MigrationAttribute>()?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        // پیشوند yyyyMMddHHmmss — ترتیب رشته‌ای همان ترتیب زمانی است
        // (نسخهٔ بدون comparer برای سازگاری عرضی FluentAssertions 6.x)
        ids.Should().BeInAscendingOrder(because: "شناسه‌های زمان‌دار باید مرتب باشند؛ ناترتیبی یعنی دو مایگریشن هم‌زمان نوشته شده‌اند");
        // طول در درخت عبارت؛ بخش عددی پیشوند بیرون آن — discard/range در Expression مجاز نیست (CS8207/CS8790)
        ids.Should().OnlyContain(id => id.Length >= 14,
            because: "پیشوند ۱۴ رقمی زمان (yyyyMMddHHmmss) الزامی است");
        foreach (var id in ids)
            long.TryParse(id[..14], out _).Should().BeTrue(because: $"پیشوند شناسهٔ {id} باید زمانِ ۱۴ رقمی باشد");
    }

    [Fact]
    public void Handwritten_Migrations_Without_Snapshot_Must_Stay_In_Known_List()
    {
        var handwritten = MigrationTypes()
            .Where(t => !HasTargetModel(t))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        handwritten.Should().BeEquivalentTo(KnownHandwrittenWithoutSnapshot,
            because: "مایگریشنِ دست‌نویسِ تازه باید یا Designer (dotnet ef migrations add) بگیرد "
                   + "یا آگاهانه به فهرست طلایی این تست اضافه شود — پیشوند [DbContext]/[Migration] "
                   + "و کشف EF را هم MigrationDiscoveryGuardTests می‌سنجد");
    }

    [Fact]
    public void Handwritten_Migrations_Must_Carry_DbContext_Attribute()
    {
        // درس چرخهٔ ۲۵: مایگریشن بدون [DbContext] هرگز کشف نمی‌شود و
        // MigrateAsync بی‌صدا هیچ‌کاری نمی‌کند → SqlException روی سرور مالک
        var bare = MigrationTypes()
            .Where(t => t.GetCustomAttribute<Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute>() is null)
            .Select(t => t.Name)
            .ToList();

        bare.Should().BeEmpty(because: "هر مایگریشن باید [DbContext(typeof(ClinicProDbContext))] داشته باشد");
    }
}

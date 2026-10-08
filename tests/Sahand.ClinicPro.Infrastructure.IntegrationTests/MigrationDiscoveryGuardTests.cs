using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// گارد رگرسیون چرخه ۲۵ — «Invalid column name 'ServiceId'» روی سرور مالک
/// </summary>
/// <remarks>
/// ریشه: مهاجرت‌های دست‌نویس (Cycle21TemplateItemsGuard و
/// Cycle23TemplateDoctorNullable و Cycle24ReceptionServices) فایل Designer.cs
/// نداشتند و مهم‌تر از آن ویژگی <c>[DbContext(typeof(ClinicProDbContext))]</c>
/// هم روی کلاس‌شان نبود. EF Core مهاجرت‌های یک اسمبلی را فقط از طریق همین
/// ویژگی به DbContext مربوط می‌کند؛ بدون آن، مهاجرت در فهرست
/// <c>GetMigrations()</c> ظاهر نمی‌شود → <c>GetPendingMigrations()</c> خالی
/// می‌ماند → <c>MigrateAsync()</c> در راه‌اندازی سرور بی‌اثر اجرا می‌شود و
/// ستون‌های جدید (ServiceId/ServiceName/PerformedByEmployeeId) هرگز به
/// دیتابیس زنده اضافه نمی‌شوند؛ نخستین پرس‌وجوی بعدی با SqlException 207
/// «Invalid column name» می‌شکند (خطای صف ویزیت سرور مالک).
/// این تست تضمین می‌کند هر مهاجرتی که در اسمبلی تعریف شده، توسط EF در
/// زمان اجرا هم دیده شود — هر مهاجرت دست‌نویسِ آیندهٔ بدون [DbContext]
/// همین‌جا و پیش از انتشار لو می‌رود.
/// </remarks>
public class MigrationDiscoveryGuardTests
{
    /// <summary>
    /// ساخت کانتکست با provider رابطه‌ای واقعی (SQLite) تا سرویس‌های
    /// رابطه‌ای EF مثل IMigrationsAssembly واقعاً ساخته شوند؛
    /// InMemory این لایه را ندارد و اتصال زنده‌ای هم برقرار نمی‌شود.
    /// </summary>
    private static ClinicProDbContext NewDesignContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicProDbContext>();
        optionsBuilder.UseSqlite("DataSource=:memory:");
        return new ClinicProDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// گارد عمومی و آینده‌نگر: هر کلاس مهاجرتِ موجود در اسمبلی زیرساخت
    /// (چه تولیدشده با ابزار، چه دست‌نویس) باید در فهرست کشفِ زمانِ اجرای
    /// EF حاضر باشد؛ نبودِ یک مهاجرت در این فهرست یعنی ویژگی
    /// [DbContext(typeof(ClinicProDbContext))] جا افتاده است.
    /// </summary>
    [Fact]
    public void Every_migration_class_in_assembly_is_discovered_by_ef_runtime()
    {
        // فهرست همهٔ مهاجرت‌های تعریف‌شده در اسمبلی — با بازتاب، بی‌هیچ فیلتری
        var assembly = typeof(ClinicProDbContext).Assembly;
        var allMigrationIds = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(typeof(Migration).IsAssignableFrom)
            .Select(t => t.GetCustomAttribute<MigrationAttribute>()?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();

        allMigrationIds.Should().NotBeEmpty("اسمبلی زیرساخت باید مهاجرت داشته باشد");

        using var db = NewDesignContext();
        var discovered = db.Database.GetMigrations().ToHashSet();

        // هستهٔ گارد: کشفِ EF باید دقیقاً همهٔ مهاجرت‌های اسمبلی را پوشش دهد
        discovered.Should().Contain(allMigrationIds,
            "هر مهاجرتِ تعریف‌شده در اسمبلی باید [DbContext(typeof(ClinicProDbContext))] داشته باشد " +
            "تا EF آن را در زمان اجرا ببیند — نبودِ ویژگی یعنی مهاجرت هرگز روی دیتابیس " +
            "زنده اعمال نمی‌شود (ریشهٔ خطای Invalid column name چرخه ۲۵)");
    }

    /// <summary>
    /// مستندسازی موردِ چرخه ۲۵ — مهاجرت‌های دست‌نویس چرخه ۲۱/۲۳/۲۴ که
    /// پیش‌تر نامرئی بودند اکنون باید کشف شوند (این تست اسم‌ها را صریح
    /// می‌گوید تا پیام شکست، نقشهٔ راه بدهد).
    /// </summary>
    [Fact]
    public void Hand_written_migrations_of_cycles_21_to_24_are_discovered()
    {
        using var db = NewDesignContext();
        var discovered = db.Database.GetMigrations().ToList();

        discovered.Should().Contain("20261008000002_Cycle21TemplateItemsGuard",
            "مهاجرت دست‌نویس چرخه ۲۱ (آزادسازی Items تمپلیت نسخه) باید کشف شود");
        discovered.Should().Contain("20261009000001_Cycle23TemplateDoctorNullable",
            "مهاجرت دست‌نویس چرخه ۲۳ (اختیاری‌کردن پزشک تمپلیت نسخه) باید کشف شود");
        discovered.Should().Contain("20261009180000_Cycle24ReceptionServices",
            "مهاجرت دست‌نویس چرخه ۲۴ (ستون‌های ServiceId/ServiceName/PerformedByEmployeeId " +
            "پذیرش خدمت درمانی) باید کشف شود");
    }
}

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// گارد رگرسیون چرخهٔ ۲۶ (گزارش هجدهم مالک) — خطای ۲۶۰۱ اس‌کیوال‌سرور:
/// «Cannot insert duplicate key row in object 'dbo.Employees' with unique
/// index 'IX_Employee_PersonnelCode'. The duplicate key value is (&lt;NULL&gt;)»
/// </summary>
/// <remarks>
/// ریشه: در اس‌کیوال‌سرور ایندکس یکتا روی ستون تهی‌پذیر فقط «یک» رکورد NULL
/// را مجاز می‌شمارد؛ ثبت دومین کاربر/پرسنلِ بدون کد پرسنلی کل تراکنش را با
/// شکست مواجه می‌کرد و پیام «خطای غیرمنتظره در سرور رخ داد» روی فرم ثبت
/// کاربر/پرسنل دیده می‌شد. راه‌حل: حلقهٔ سراسری <c>OnModelCreating</c> در
/// ClinicProDbContext که فیلتر «[ستون] IS NOT NULL» را روی «همهٔ» ایندکس‌های
/// یکتای تک‌ستونیِ تهی‌پذیر می‌نشاند (مهاجرت Cycle26NullableUniqueIndexes).
/// این تست‌ها تضمین می‌کنند قاعدهٔ مذکور هیچ‌گاه در آینده فراموش یا دور
/// زده نشود — هر ایندکسِ تازهٔ معیوب همین‌جا و پیش از انتشار لو می‌رود.
/// </remarks>
public class NullableUniqueIndexFilterTests
{
    /// <summary>
    /// ساخت کانتکست با provider رابطه‌ای (SQLite درون‌حافظه‌ای) تا مدلِ
    /// رابطه‌ای کامل ساخته شود و فیلترهای ایندکس قابل بازرسی باشند؛
    /// اتصال زنده‌ای برقرار نمی‌شود و تست کاملاً ایزوله است.
    /// </summary>
    private static ClinicProDbContext NewDesignContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicProDbContext>();
        optionsBuilder.UseSqlite("DataSource=:memory:");
        return new ClinicProDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// گارد عمومی و آینده‌نگر — هر ایندکس یکتای تک‌ستونی که ستونش تهی‌پذیر
    /// است باید فیلتری شامل «IS NOT NULL» داشته باشد؛ در غیر این صورت در
    /// اس‌کیوال‌سرور فقط یک رکورد NULL قابل ثبت است و خطای ۲۶۰۱ برمی‌گردد.
    /// </summary>
    [Fact]
    public void Every_nullable_unique_single_column_index_has_IsNotNull_filter()
    {
        using var context = NewDesignContext();

        var offenders = new List<string>();
        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
                continue; // موجودیت بدون جدول — بی‌ربط

            var tableId = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var index in entityType.GetIndexes())
            {
                // فقط ایندکس‌های یکتای تک‌ستونی با ستون تهی‌پذیر هدف‌اند
                if (!index.IsUnique || index.Properties.Count != 1)
                    continue;

                var prop = index.Properties[0];
                if (!prop.IsNullable)
                    continue;

                var column = prop.GetColumnName(tableId);
                if (string.IsNullOrEmpty(column))
                    continue;

                var filter = index.GetFilter();
                if (filter is null || !filter.Contains("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
                    offenders.Add($"{tableName}.{index.GetDatabaseName()} → فیلتر: {filter ?? "(بدون فیلتر)"}");
            }
        }

        // پیام خطا فهرست کامل ایندکس‌های معیوب را نشان می‌دهد تا رفعش سریع باشد
        offenders.Should().BeEmpty(
            "هر ایندکس یکتای تک‌ستونیِ تهی‌پذیر باید فیلتر IS NOT NULL داشته باشد تا چند رکورد NULL قابل ثبت باشد (خطای ۲۶۰۱ سرور)");
    }

    /// <summary>
    /// مورد مشخصِ گزارش مالک — ایندکس یکتای PersonnelCode پرسنل باید سطرهای
    /// NULL را از دامنهٔ یکتایی خارج کند و فیلتر حذف نرم (IsDeleted) را هم
    /// حفظ کرده باشد؛ در غیر این صورت ثبت پرسنل بدون کد پرسنلی (یا ایجاد
    /// رکورد حداقلی پرسنل هنگام ثبت کاربر) دوباره با خطای ۲۶۰۱ می‌شکند.
    /// </summary>
    [Fact]
    public void Employee_PersonnelCode_unique_index_excludes_null_rows_and_keeps_soft_delete_filter()
    {
        using var context = NewDesignContext();

        var employee = context.Model.FindEntityType(typeof(Employee));
        employee.Should().NotBeNull("موجودیت Employee باید در مدل باشد");

        var index = employee!.GetIndexes()
            .Single(i => i.GetDatabaseName() == "IX_Employee_PersonnelCode");

        var filter = index.GetFilter();
        filter.Should().NotBeNull("ایندکس PersonnelCode تهی‌پذیر است و باید فیلتر داشته باشد");
        filter.Should().Contain("IS NOT NULL", "سطرهای NULL نباید در دامنهٔ یکتایی باشند");
        filter.Should().Contain("IsDeleted", "فیلتر حذف نرم موجود باید حفظ شود");
    }

    /// <summary>
    /// مورد محوری دومِ همان گزارش — NationalCode پرسنل نیز تهی‌پذیر است و
    /// همان بیمهٔ یکتایی را باید داشته باشد؛ این تست جداگانه تا حذف تصادفیِ
    /// فیلتر این ستون در بازسازی‌های آینده هم لو برود.
    /// </summary>
    [Fact]
    public void Employee_NationalCode_unique_index_excludes_null_rows()
    {
        using var context = NewDesignContext();

        var employee = context.Model.FindEntityType(typeof(Employee));
        var index = employee!.GetIndexes()
            .Single(i => i.GetDatabaseName() == "IX_Employee_NationalCode");

        index.GetFilter().Should().NotBeNull();
        index.GetFilter().Should().Contain("IS NOT NULL");
    }
}

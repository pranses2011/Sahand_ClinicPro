using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// گارد رگرسیون نصب سرور — «Property 'MS_Description' already exists» (SQL 15233)
/// </summary>
/// <remarks>
/// <para>
/// گزارش مالک (v2.22.5): <c>Install-Server-Package.ps1</c> در گام «ساخت دیتابیس و
/// اعمال خودکار تغییرهای لازم» با این خطا متوقف می‌شد و نصب کامل نمی‌شد:
/// <c>SqlException 15233 — Property cannot be added. Property 'MS_Description'
/// already exists for 'dbo.InsuranceCompanies.IsContracted'</c>.
/// </para>
/// <para>
/// ریشه: EF Core در <c>SqlServerMigrationsSqlGenerator</c> فقط وقتی فرمان توضیح
/// ستون را می‌نویسد که <c>OldColumn.Comment != Comment</c> باشد؛ اگر مهاجرت
/// پارامتر <c>oldComment:</c> نداشته باشد، OldColumn.Comment تهی است و EF فرمان
/// <c>EXEC sp_addextendedproperty</c> را برای ستونی می‌نویسد که توضیحش از قبل
/// موجود است → SQL Server آن را با ۱۵۲۳۳ رد می‌کند → تراکنش مهاجرت عقب می‌گردد و
/// «کل» مهاجرت اعمال نمی‌شود.
/// </para>
/// <para>
/// این تست سه لایه را می‌سنجد: (۱) خودِ مهاجرت آسیب‌دیده اصلاح شده باشد؛
/// (۲) هیچ مهاجرت دیگری در کل تاریخچه «افزودنِ توضیحِ موجود» تولید نکند؛
/// (۳) تولیدگر ثبت‌شده در DI افزودن/حذف توضیح را idempotent انجام دهد.
/// </para>
/// </remarks>
public class MigrationCommentIdempotencyTests
{
    /// <summary>پیکربندی درون‌حافظه‌ای با رشته اتصال آزمایشی (بدون اتصال واقعی)</summary>
    private static IConfiguration BuildConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=localhost;Database=SahandClinicProDb;Trusted_Connection=True;TrustServerCertificate=True"
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    /// <summary>کانتکست SQL Server برای تولید SQL — بدون هیچ اتصال زنده‌ای</summary>
    private static ClinicProDbContext NewSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlServer("Server=.;Database=ClinicProGuard;Trusted_Connection=True;TrustServerCertificate=True")
            .ReplaceService<IMigrationsSqlGenerator, ClinicProMigrationsSqlGenerator>()
            .Options;
        return new ClinicProDbContext(options);
    }

    /// <summary>SQL تولیدشده برای یک عملیات AlterColumn (تغییر nullable + توضیح)</summary>
    private static string GenerateAlterColumnSql(string? oldComment, string newComment)
    {
        using var db = NewSqlServerContext();
        _ = db.Model; // ساخت مدل و راه‌اندازی سرویس‌های درونی EF
        var generator = db.GetService<IMigrationsSqlGenerator>();

        var operations = new List<MigrationOperation>
        {
            new AlterColumnOperation
            {
                Table = "InsuranceCompanies",
                Name = "IsContracted",
                ClrType = typeof(bool),
                ColumnType = "bit",
                IsNullable = false,
                DefaultValue = false,
                Comment = newComment,
                OldColumn = new AddColumnOperation
                {
                    Table = "InsuranceCompanies",
                    Name = "IsContracted",
                    ClrType = typeof(bool),
                    ColumnType = "bit",
                    IsNullable = true,
                    Comment = oldComment
                }
            }
        };

        return string.Join(Environment.NewLine,
            generator.Generate(operations, db.Model).Select(c => c.CommandText));
    }

    /// <summary>
    /// مهاجرت آسیب‌دیده (AddInsuranceContractStatus) باید وضعیت قبلیِ توضیح ستون را
    /// اعلام کند تا EF هیچ فرمان توضیحی برای AlterColumn تولید نکند.
    /// </summary>
    [Fact]
    public void AddInsuranceContractStatus_Should_Declare_OldColumn_Comment()
    {
        var operations = new AddInsuranceContractStatus().UpOperations;

        var alter = operations
            .OfType<AlterColumnOperation>()
            .Single(o => o.Table == "InsuranceCompanies" && o.Name == "IsContracted");

        alter.Comment.Should().NotBeNullOrEmpty(
            because: "ستون باید توضیح «طرف قرارداد درمانگاه» را نگه دارد");
        alter.OldColumn.Comment.Should().Be(alter.Comment,
            because: "اگر وضعیت قبلی اعلام نشود، EF دوباره sp_addextendedproperty می‌نویسد " +
                     "و SQL Server با خطای 15233 نصب سرور را متوقف می‌کند");
    }

    /// <summary>
    /// شبیه‌سازی کل تاریخچهٔ مهاجرت‌ها: توضیحِ هر ستون پس از هر مهاجرت ردیابی می‌شود و
    /// هر AlterColumn که «توضیحِ از قبل موجود» را دوباره بیفزاید، تخلف است.
    /// </summary>
    [Fact]
    public void No_Migration_Should_Re_Add_An_Existing_Column_Comment()
    {
        // توضیح جاریِ هر ستون در دیتابیس — کلید: (جدول، ستون)؛ ستون null یعنی توضیح جدول
        var descriptions = new Dictionary<(string Table, string? Column), string?>();
        var violations = new List<string>();

        var migrations = typeof(ClinicProDbContext).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => typeof(Migration).IsAssignableFrom(t))
            .Select(t => (Type: t, Id: t.GetCustomAttributes(typeof(MigrationAttribute), false)
                .OfType<MigrationAttribute>().FirstOrDefault()?.Id ?? string.Empty))
            .Where(x => x.Id.Length > 0)
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var (type, id) in migrations)
        {
            var migration = (Migration)Activator.CreateInstance(type)!;
            IReadOnlyList<MigrationOperation> operations;
            try
            {
                operations = migration.UpOperations;
            }
            catch (Exception)
            {
                // مهاجرتی که Up آن بدون provider قابل ساخت نیست، در این شبیه‌سازی رد می‌شود؛
                // کشف/اجرا بودنش را MigrationDiscoveryGuardTests می‌سنجد.
                continue;
            }

            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case CreateTableOperation create:
                        if (create.Comment != null)
                            descriptions[(create.Name, null)] = create.Comment;
                        foreach (var column in create.Columns)
                            if (column.Comment != null)
                                descriptions[(create.Name, column.Name)] = column.Comment;
                        break;

                    case AddColumnOperation add when add.Comment != null:
                        descriptions[(add.Table, add.Name)] = add.Comment;
                        break;

                    case AlterColumnOperation alter:
                        var key = (alter.Table, alter.Name);
                        var known = descriptions.TryGetValue(key, out var current) ? current : null;
                        if (alter.Comment != null
                            && known != null
                            && known == alter.Comment
                            && alter.OldColumn.Comment != alter.Comment)
                        {
                            violations.Add(
                                $"{id}: ستون {alter.Table}.{alter.Name} توضیح «{alter.Comment}» را از قبل دارد " +
                                "ولی oldComment اعلام نشده است → EF دوباره sp_addextendedproperty می‌نویسد (خطای 15233)");
                        }

                        // به‌روزرسانی وضعیت، دقیقاً مانند منطق EF
                        if (alter.OldColumn.Comment != alter.Comment)
                            descriptions[key] = alter.Comment;
                        break;

                    case DropColumnOperation drop:
                        descriptions.Remove((drop.Table, drop.Name));
                        break;

                    case AlterTableOperation alterTable:
                        if (alterTable.OldTable.Comment != alterTable.Comment)
                            descriptions[(alterTable.Name, null)] = alterTable.Comment;
                        break;

                    case DropTableOperation dropTable:
                        foreach (var stale in descriptions.Keys.Where(k => k.Table == dropTable.Name).ToList())
                            descriptions.Remove(stale);
                        break;
                }
            }
        }

        violations.Should().BeEmpty(
            because: "هر مهاجرت که توضیح موجود ستون را دوباره «بیفزاید» نصب/ارتقای سرور را با خطای " +
                     "SQL 15233 متوقف می‌کند — پارامتر oldComment را برابر comment بگذارید");
    }

    /// <summary>
    /// تولیدگر ثبت‌شده در کانتینر واقعی تزریق وابستگی باید همان تولیدگر idempotent
    /// این پروژه باشد تا مسیر «اعمال خودکار مهاجرت‌ها در راه‌اندازی» هم پوشش یابد.
    /// </summary>
    [Fact]
    public void AddInfrastructure_Should_Register_Idempotent_Migrations_Sql_Generator()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(BuildConfiguration());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        _ = db.Model;

        db.GetService<IMigrationsSqlGenerator>()
            .Should().BeOfType<ClinicProMigrationsSqlGenerator>(
                because: "تولیدگر پیش‌فرض EF برای توضیحِ ستونِ موجود، sp_addextendedproperty تکراری " +
                         "می‌نویسد و نصب سرور با خطای 15233 شکست می‌خورد");
    }

    /// <summary>
    /// همان سناریوی گزارش‌شده: AlterColumn ستونی که توضیحش از قبل موجود است ولی
    /// مهاجرت oldComment نداده → SQL باید «به‌روزرسانی» کند، نه دوباره بیفزاید.
    /// </summary>
    [Fact]
    public void AlterColumn_With_Existing_Comment_Should_Update_Instead_Of_Add()
    {
        var sql = GenerateAlterColumnSql(oldComment: null, newComment: "طرف قرارداد درمانگاه");

        sql.Should().Contain("IF EXISTS (SELECT 1 FROM [sys].[extended_properties]",
            because: "افزودن توضیح باید با بررسی وجود آن محافظت شود");
        sql.Should().Contain("EXEC sp_updateextendedproperty 'MS_Description'",
            because: "اگر توضیح از قبل هست باید به‌روزرسانی شود (خطای 15233 رخ ندهد)");
        sql.Should().Contain("EXEC sp_addextendedproperty 'MS_Description'",
            because: "اگر توضیح نبود، باید افزوده شود (نصب تازه)");
        sql.Should().Contain("ALTER TABLE [InsuranceCompanies] ALTER COLUMN [IsContracted]",
            because: "اصل تغییر ستون باید دست‌نخورده بماند");
    }

    /// <summary>
    /// توضیح تغییرکرده: EF ابتدا حذف و سپس (بدون اعلان متغیر تازه) افزودن را می‌نویسد؛
    /// حذف هم باید با وجودسنجی محافظت شود تا خطای 15197 (توضیح ناموجود) رخ ندهد.
    /// </summary>
    [Fact]
    public void AlterColumn_With_Changed_Comment_Should_Guard_Drop_And_Add()
    {
        var sql = GenerateAlterColumnSql(oldComment: "توضیح قبلی", newComment: "توضیح تازه");

        sql.Should().Contain("EXEC sp_dropextendedproperty 'MS_Description'");
        sql.Should().Contain("IF EXISTS (SELECT 1 FROM [sys].[extended_properties]",
            because: "حذف توضیح ناموجود در SQL Server خطای 15197 می‌دهد");
        sql.Should().Contain("EXEC sp_addextendedproperty 'MS_Description'");
    }
}

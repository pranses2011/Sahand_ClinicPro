using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>
/// تولیدگر SQL مهاجرت‌های SQL Server برای درمانگاه سهند — «توضیح جدول/ستون» تکرارپذیر (idempotent)
/// </summary>
/// <remarks>
/// ═══ ریشهٔ خطای نصب سرور (خطای ۱۵۲۳۳): «Property 'MS_Description' already exists» ═══
/// <para>
/// گزارش مالک: هنگام اجرای <c>Install-Server-Package.ps1</c>، ساخت خودکار دیتابیس با
/// <c>SqlException 15233 — Property cannot be added. Property 'MS_Description' already
/// exists for 'dbo.InsuranceCompanies.IsContracted'</c> شکست می‌خورد و کل نصب متوقف
/// می‌شد. SQL شکست‌خورده همان چیزی است که EF Core برای
/// <c>AlterColumnOperation</c> تولید می‌کند: ALTER COLUMN + DEFAULT + در پایان
/// <c>EXEC sp_addextendedproperty 'MS_Description' …</c>.
/// </para>
/// <para>
/// چرا این فرمان تولید می‌شود؟ EF Core در <c>SqlServerMigrationsSqlGenerator</c>
/// (نسخهٔ 8.0.x، متد <c>AlterColumnOperation</c>) این شرط را دارد:
/// <c>if (operation.OldColumn.Comment != operation.Comment)</c> و سپس اگر توضیح تازه
/// تهی نباشد، «همیشه» <c>sp_addextendedproperty</c> را می‌نویسد. یعنی برای این‌که
/// فرمانِ افزودنِ توضیح صادر شود، کافی است مهاجرت پارامتر <c>oldComment:</c> را
/// نداشته باشد (<c>OldColumn.Comment = null</c>) در حالی که ستون در دیتابیس
/// «از قبل» توضیح دارد. مهاجرت‌های این پروژه بخشی‌شان دست‌نویس‌اند
/// (بدون <c>dotnet ef migrations add</c>؛ نگاه کنید به فهرست طلایی
/// <c>MigrationSnapshotComplianceTests</c>) و در آن‌ها به‌سادگی ممکن است
/// <c>AlterColumn(…, comment: "…")</c> بدون <c>oldComment:</c> نوشته شود —
/// دقیقاً همین اتفاق در مهاجرت <c>20261023120000_AddInsuranceContractStatus</c>
/// (نسخهٔ 2.22.5) رخ داد: همان مهاجرت، ستون <c>InsuranceCompanies.IsContracted</c>
/// را با توضیح «طرف قرارداد درمانگاه» ساخت و سپس در AlterColumn همان ستون
/// (nullable → NOT NULL) توضیح را دوباره «افزود» — SQL Server با خطای ۱۵۲۳۳
/// پاسخ داد، تراکنش مهاجرت عقب گشت و نصب سرور شکست خورد.
/// آن مهاجرت هم مستقیماً اصلاح شد (افزودن <c>oldComment:</c>).
/// این تولیدگر «لایهٔ دومِ» دفاعی است: هر مهاجرت آیندهٔ دست‌نویس با همین
/// الگو (توضیح بدون <c>oldComment:</c>) هم دیگر نصب را نمی‌شکند.
/// </para>
/// <para>
/// ═══ رفع: «افزودن یا به‌روزرسانی» به‌جای «افزودن کور» ═══
/// دو متد <c>AddDescription</c> و <c>DropDescription</c> در تولیدگر SQL Server
/// <c>protected virtual</c> هستند؛ این‌جا بازنویسی می‌شوند تا فرمان‌ها به‌جای
/// <c>EXEC sp_addextendedproperty …</c> بی‌قید، به این شکل تولید شوند:
/// <code>
/// IF EXISTS (SELECT 1 FROM [sys].[extended_properties] …)
/// BEGIN
///     EXEC sp_updateextendedproperty 'MS_Description', @description, …
/// END
/// ELSE
/// BEGIN
///     EXEC sp_addextendedproperty 'MS_Description', @description, …
/// END
/// </code>
/// و برای حذف هم <c>IF EXISTS … EXEC sp_dropextendedproperty …</c> (رفع خطای
/// ۱۵۱۹۷ «Extended property not found» در دیتابیس‌هایی که توضیح ستون را
/// ندارند). نتیجه: اجرای مهاجرت‌ها «تکرارپذیر» می‌شود — چه توضیح از قبل
/// موجود باشد، چه نباشد، چه متنش عوض شده باشد، نتیجه یکی است و هیچ نصبی
/// دیگر به‌خاطر تکراری‌بودن توضیحِ ستون شکست نمی‌خورد.
/// </para>
/// <para>
/// چرا این کار امن است؟ توضیح جدول/ستون (<c>MS_Description</c>) فقط
/// فرادادهٔ مستندسازی است و در رفتار داده‌ای هیچ نقشی ندارد؛ بنابراین تبدیل
/// «افزودن» به «افزودن-یا-به‌روزرسانی» هیچ تغییری در داده یا اسکیمای
/// واقعی جداول نمی‌دهد — تنها از شکستنِ مهاجرت جلوگیری می‌کند.
/// </para>
/// <para>
/// ثبت: در <c>AddInfrastructure</c> با
/// <c>options.ReplaceService&lt;IMigrationsSqlGenerator, ClinicProMigrationsSqlGenerator&gt;()</c>
/// و همچنین در <c>DesignTimeDbContextFactory</c> (تا اسکریپت‌های تولیدشده با
/// <c>dotnet ef migrations script</c> هم همین رفتار تکرارپذیر را داشته باشند).
/// </para>
/// </remarks>
public class ClinicProMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
    /// <summary>
    /// ساخت تولیدگر — همان وابستگی‌های تولیدگر پیش‌فرض SQL Server
    /// </summary>
    /// <param name="dependencies">وابستگی‌های مشترک تولیدگر مهاجرت</param>
    /// <param name="commandBatchPreparer">آماده‌ساز دسته‌های فرمان (برای بازنویسی عملیات)</param>
    public ClinicProMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        ICommandBatchPreparer commandBatchPreparer)
        : base(dependencies, commandBatchPreparer)
    {
    }

    /// <summary>
    /// افزودن توضیح (<c>MS_Description</c>) به جدول یا ستون — به‌صورت
    /// «افزودن در صورت نبود، به‌روزرسانی در صورت وجود» تا خطای ۱۵۲۳۳
    /// (توضیح تکراری) هرگز رخ ندهد.
    /// </summary>
    /// <param name="builder">سازندهٔ فرمان مهاجرت</param>
    /// <param name="description">متن توضیح (فارسی — یونیکد)</param>
    /// <param name="schema">اسکیمای جدول؛ <c>null</c> یعنی اسکیمای پیش‌فرض کاربر</param>
    /// <param name="table">نام جدول</param>
    /// <param name="column">نام ستون؛ <c>null</c> یعنی توضیح خودِ جدول</param>
    /// <param name="omitVariableDeclarations">
    /// اگر <c>true</c> باشد، متغیرهای <c>@defaultSchema</c>/<c>@description</c>
    /// از قبل (در فرمان حذفِ همین دسته) اعلان شده‌اند و دوباره اعلان نمی‌شوند
    /// </param>
    protected override void AddDescription(
        MigrationCommandListBuilder builder,
        string description,
        string? schema,
        string table,
        string? column = null,
        bool omitVariableDeclarations = false)
    {
        var terminator = Dependencies.SqlGenerationHelper.StatementTerminator;
        var stringMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var schemaLiteral = DeclareAndGetSchemaLiteral(builder, schema, omitVariableDeclarations, terminator, stringMapping);

        if (!omitVariableDeclarations)
        {
            builder.Append("DECLARE @description AS sql_variant").AppendLine(terminator);
        }

        builder
            .Append("SET @description = ")
            .Append(stringMapping.GenerateSqlLiteral(description))
            .AppendLine(terminator);

        // فرمان «افزودن یا به‌روزرسانی» — وجود توضیح با sys.extended_properties سنجیده می‌شود:
        //   class = 1 (OBJECT_OR_COLUMN)
        //   major_id = شناسهٔ جدول، minor_id = شناسهٔ ستون (یا ۰ برای خود جدول)
        builder.AppendLine(BuildExistencePredicate(table, schema, column, stringMapping));
        builder.AppendLine("BEGIN");
        AppendExtendedPropertyCall(builder, "sp_updateextendedproperty", schemaLiteral, table, column, stringMapping, terminator);
        builder.AppendLine("END");
        builder.AppendLine("ELSE");
        builder.AppendLine("BEGIN");
        AppendExtendedPropertyCall(builder, "sp_addextendedproperty", schemaLiteral, table, column, stringMapping, terminator);
        builder.AppendLine("END");
    }

    /// <summary>
    /// حذف توضیح (<c>MS_Description</c>) جدول یا ستون — فقط اگر واقعاً وجود
    /// داشته باشد؛ تا خطای ۱۵۱۹۷ («Extended property not found») رخ ندهد.
    /// </summary>
    /// <param name="builder">سازندهٔ فرمان مهاجرت</param>
    /// <param name="schema">اسکیمای جدول؛ <c>null</c> یعنی اسکیمای پیش‌فرض کاربر</param>
    /// <param name="table">نام جدول</param>
    /// <param name="column">نام ستون؛ <c>null</c> یعنی توضیح خودِ جدول</param>
    /// <param name="omitVariableDeclarations">
    /// اگر <c>true</c> باشد، متغیرهای مشترک از قبل اعلان شده‌اند
    /// </param>
    protected override void DropDescription(
        MigrationCommandListBuilder builder,
        string? schema,
        string table,
        string? column = null,
        bool omitVariableDeclarations = false)
    {
        var terminator = Dependencies.SqlGenerationHelper.StatementTerminator;
        var stringMapping = Dependencies.TypeMappingSource.GetMapping(typeof(string));
        var schemaLiteral = DeclareAndGetSchemaLiteral(builder, schema, omitVariableDeclarations, terminator, stringMapping);

        // اعلان @description عیناً مانند تولیدگر پایه حفظ می‌شود؛ زیرا در جریان
        // AlterColumn، متد AddDescription با omitVariableDeclarations=true
        // بلافاصله پس از همین متد اجرا می‌شود و به این اعلان تکیه دارد.
        if (!omitVariableDeclarations)
        {
            builder.Append("DECLARE @description AS sql_variant").AppendLine(terminator);
        }

        builder.AppendLine(BuildExistencePredicate(table, schema, column, stringMapping));
        builder.AppendLine("BEGIN");
        AppendDropExtendedPropertyCall(builder, schemaLiteral, table, column, stringMapping, terminator);
        builder.AppendLine("END");
    }

    /// <summary>
    /// اعلان <c>@defaultSchema</c> (در صورت نیاز) و برگرداندن عبارت اسکیمای
    /// مورد استفاده در فراخوانی رویه‌های توضیح
    /// </summary>
    private static string DeclareAndGetSchemaLiteral(
        MigrationCommandListBuilder builder,
        string? schema,
        bool omitVariableDeclarations,
        string terminator,
        RelationalTypeMapping stringMapping)
    {
        if (schema != null)
        {
            return stringMapping.GenerateSqlLiteral(schema);
        }

        if (!omitVariableDeclarations)
        {
            builder.Append("DECLARE @defaultSchema AS sysname").AppendLine(terminator);
            builder.Append("SET @defaultSchema = SCHEMA_NAME()").AppendLine(terminator);
        }

        return "@defaultSchema";
    }

    /// <summary>
    /// ساخت شرط وجود توضیح <c>MS_Description</c> برای یک جدول یا ستون —
    /// <c>IF EXISTS (SELECT 1 FROM [sys].[extended_properties] …)</c>
    /// </summary>
    private string BuildExistencePredicate(
        string table,
        string? schema,
        string? column,
        RelationalTypeMapping stringMapping)
    {
        // OBJECT_ID با نام کاملِ دلیمیت‌شده — همان قالبی که خود EF برای [dbo].[Table] می‌سازد
        var objectName = Dependencies.SqlGenerationHelper.DelimitIdentifier(table, schema);
        var objectNameLiteral = stringMapping.GenerateSqlLiteral(objectName);

        var minorId = column == null
            ? "0"
            : $"COLUMNPROPERTY(OBJECT_ID({objectNameLiteral}), {stringMapping.GenerateSqlLiteral(column)}, 'ColumnId')";

        var predicate = new StringBuilder();
        predicate
            .Append("IF EXISTS (SELECT 1 FROM [sys].[extended_properties] AS [ep]")
            .Append(" WHERE [ep].[class] = 1")
            .Append(" AND [ep].[name] = N'MS_Description'")
            .Append(" AND [ep].[major_id] = OBJECT_ID(").Append(objectNameLiteral).Append(')')
            .Append(" AND [ep].[minor_id] = ").Append(minorId)
            .Append(')');

        return predicate.ToString();
    }

    /// <summary>
    /// نوشتن فراخوانی <c>sp_addextendedproperty</c> یا <c>sp_updateextendedproperty</c>
    /// با همان ترتیب و قالب آرگومان‌های تولیدگر پایه EF
    /// </summary>
    private static void AppendExtendedPropertyCall(
        MigrationCommandListBuilder builder,
        string procedure,
        string schemaLiteral,
        string table,
        string? column,
        RelationalTypeMapping stringMapping,
        string terminator)
    {
        builder
            .Append("    EXEC ").Append(procedure)
            .Append(" 'MS_Description', @description, 'SCHEMA', ").Append(schemaLiteral)
            .Append(", 'TABLE', ").Append(stringMapping.GenerateSqlLiteral(table));

        if (column != null)
        {
            builder
                .Append(", 'COLUMN', ")
                .Append(stringMapping.GenerateSqlLiteral(column));
        }

        builder.AppendLine(terminator);
    }

    /// <summary>
    /// نوشتن فراخوانی <c>sp_dropextendedproperty</c> در همان قالب تولیدگر پایه EF
    /// </summary>
    private static void AppendDropExtendedPropertyCall(
        MigrationCommandListBuilder builder,
        string schemaLiteral,
        string table,
        string? column,
        RelationalTypeMapping stringMapping,
        string terminator)
    {
        builder
            .Append("    EXEC sp_dropextendedproperty 'MS_Description', 'SCHEMA', ")
            .Append(schemaLiteral)
            .Append(", 'TABLE', ")
            .Append(stringMapping.GenerateSqlLiteral(table));

        if (column != null)
        {
            builder
                .Append(", 'COLUMN', ")
                .Append(stringMapping.GenerateSqlLiteral(column));
        }

        builder.AppendLine(terminator);
    }
}

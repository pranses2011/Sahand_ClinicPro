using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ClinicProDbContext))]
[Migration("20261023120000_AddInsuranceContractStatus")]
public partial class AddInsuranceContractStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsContracted",
            table: "InsuranceCompanies",
            type: "bit",
            nullable: true,
            comment: "طرف قرارداد درمانگاه");

        // داده‌های موجود متعلق به بیمه‌های قبلاً پیکربندی‌شده‌اند؛ برای سازگاری
        // rollout اولیه، قبلی‌ها طرف قرارداد فرض می‌شوند و شرکت‌های جدید خیر.
        migrationBuilder.Sql("UPDATE [InsuranceCompanies] SET [IsContracted] = 1 WHERE [IsContracted] IS NULL;");

        // ═══ رفع خطای نصب سرور ۱۵۲۳۳ «Property 'MS_Description' already exists» ═══
        // ستون در همین مهاجرت (AddColumn بالا) با توضیح «طرف قرارداد درمانگاه»
        // ساخته شد؛ پس وضعیت «قبلی» ستون هم همان توضیح است. بدون پارامتر
        // oldComment، مقدار OldColumn.Comment تهی می‌ماند و EF Core برای این
        // AlterColumn دوباره EXEC sp_addextendedproperty می‌نویسد → SQL Server با
        // 15233 رد می‌کند → تراکنش مهاجرت عقب می‌گردد و نصب کامل شکست می‌خورد
        // (گزارش مالک: Install-Server-Package.ps1 با کد -532462766). با
        // oldComment برابرِ comment، EF هیچ فرمانِ توضیحی تولید نمی‌کند و فقط
        // ALTER COLUMN + DEFAULT اجرا می‌شود.
        migrationBuilder.AlterColumn<bool>(
            name: "IsContracted",
            table: "InsuranceCompanies",
            type: "bit",
            nullable: false,
            defaultValue: false,
            comment: "طرف قرارداد درمانگاه",
            oldClrType: typeof(bool),
            oldType: "bit",
            oldNullable: true,
            oldComment: "طرف قرارداد درمانگاه");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsContracted", table: "InsuranceCompanies");
    }
}

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

        migrationBuilder.AlterColumn<bool>(
            name: "IsContracted",
            table: "InsuranceCompanies",
            type: "bit",
            nullable: false,
            defaultValue: false,
            comment: "طرف قرارداد درمانگاه",
            oldClrType: typeof(bool),
            oldType: "bit",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsContracted", table: "InsuranceCompanies");
    }
}

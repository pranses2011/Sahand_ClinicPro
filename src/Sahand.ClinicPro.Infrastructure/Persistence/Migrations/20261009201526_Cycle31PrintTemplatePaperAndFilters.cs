using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle31PrintTemplatePaperAndFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultCoveragePercent",
                table: "InsuranceCompanies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                comment: "درصد پوشش پیش‌فرض بیمه",
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultCoveragePercent",
                table: "InsuranceCompanies",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldComment: "درصد پوشش پیش‌فرض بیمه");
        }
    }
}

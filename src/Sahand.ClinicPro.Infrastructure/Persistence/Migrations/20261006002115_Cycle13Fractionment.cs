using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13Fractionment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceFractionmentRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceCompanyId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    RuleType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع قاعده — ۱=درصدی ۲=اسمی"),
                    InsurancePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, comment: "درصد سهم بیمه (وقتی درصدی)"),
                    NominalAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "مبلغ اسمی سهم بیمه (وقتی اسمی)"),
                    FranchiseAmount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "فرانشیز ثابت بیمار"),
                    PerItemCeiling = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false, comment: "سقف پرداخت بیمه به‌ازای قلم — صفر یعنی بدون سقف"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت قرارداد"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceFractionmentRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceFractionmentRules_InsuranceCompanies_InsuranceCompanyId",
                        column: x => x.InsuranceCompanyId,
                        principalTable: "InsuranceCompanies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InsuranceFractionmentRules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                },
                comment: "قاعده تسویه بیمه — درصدی/اسمی به‌ازای خدمت + فرانشیز + سقف");

            migrationBuilder.CreateIndex(
                name: "IX_FractionmentRule_Company_Service",
                table: "InsuranceFractionmentRules",
                columns: new[] { "InsuranceCompanyId", "ServiceId", "IsActive" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceFractionmentRules_ServiceId",
                table: "InsuranceFractionmentRules",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceFractionmentRules");
        }
    }
}

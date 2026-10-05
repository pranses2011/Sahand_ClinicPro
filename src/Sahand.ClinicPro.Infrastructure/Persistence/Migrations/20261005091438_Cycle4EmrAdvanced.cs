using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle4EmrAdvanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultCoveragePercent",
                table: "InsuranceCompanies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ChronicCarePlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DiseaseTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان بیماری مزمن"),
                    Icd10Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "کد ICD-10"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextReviewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Goals = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "اهداف درمانی قابل اندازه‌گیری"),
                    Actions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "اقدامات جاری"),
                    CareTeam = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "اعضای تیم مراقبت"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت آزاد"),
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
                    table.PrimaryKey("PK_ChronicCarePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicCarePlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "طرح مراقبت بیمار مزمن — چرخه هدف/اقدام/بازبینی");

            migrationBuilder.CreateTable(
                name: "FamilyHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    Relative = table.Column<byte>(type: "tinyint", nullable: false),
                    Disease = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "عنوان بیماری"),
                    Icd10Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "کد ICD-10"),
                    OnsetAge = table.Column<int>(type: "int", nullable: true),
                    IsGenetic = table.Column<bool>(type: "bit", nullable: false),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DeathAge = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت آزاد"),
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
                    table.PrimaryKey("PK_FamilyHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "سابقه پزشکی خانواده — خویشاوندان بیمار");

            migrationBuilder.CreateTable(
                name: "GrowthMeasurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    MeasuredAt = table.Column<DateOnly>(type: "date", nullable: false),
                    AgeMonths = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true, comment: "وزن کیلوگرم"),
                    HeightCm = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true, comment: "قد سانتی‌متر"),
                    HeadCircumferenceCm = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true, comment: "محیط سر سانتی‌متر"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت شرایط اندازه‌گیری"),
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
                    table.PrimaryKey("PK_GrowthMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrowthMeasurements_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "اندازه‌گیری رشد کودک — وزن/قد/محیط سر");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicCarePlans_Patient",
                table: "ChronicCarePlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicCarePlans_Review",
                table: "ChronicCarePlans",
                columns: new[] { "Status", "NextReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyHistories_Genetic",
                table: "FamilyHistories",
                column: "IsGenetic");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyHistories_Patient",
                table: "FamilyHistories",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthMeasurements_Patient",
                table: "GrowthMeasurements",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_GrowthMeasurements_Patient_Date",
                table: "GrowthMeasurements",
                columns: new[] { "PatientId", "MeasuredAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChronicCarePlans");

            migrationBuilder.DropTable(
                name: "FamilyHistories");

            migrationBuilder.DropTable(
                name: "GrowthMeasurements");

            migrationBuilder.DropColumn(
                name: "DefaultCoveragePercent",
                table: "InsuranceCompanies");
        }
    }
}

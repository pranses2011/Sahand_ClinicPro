using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13Archive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchiveRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "نام جدول مبدأ"),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "نام بیمار هنگام آرشیو"),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    YearJalali = table.Column<int>(type: "int", nullable: false),
                    SummaryLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "خلاصه‌سطر نمایشی"),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "تصویر کامل ردیف JSON"),
                    ArchivedBy = table.Column<long>(type: "bigint", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ArchiveRecords", x => x.Id);
                },
                comment: "رکورد آرشیو سالانه — تصویر JSON ردیف‌های قدیمی");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveRecords_PatientId_SourceTable",
                table: "ArchiveRecords",
                columns: new[] { "PatientId", "SourceTable" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveRecords_RecordDate",
                table: "ArchiveRecords",
                column: "RecordDate");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveRecords_SourceTable_SourceId",
                table: "ArchiveRecords",
                columns: new[] { "SourceTable", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveRecords_YearJalali",
                table: "ArchiveRecords",
                column: "YearJalali");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveRecords");
        }
    }
}

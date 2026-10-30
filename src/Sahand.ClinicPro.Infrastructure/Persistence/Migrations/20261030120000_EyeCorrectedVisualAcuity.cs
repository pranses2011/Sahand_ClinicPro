using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>ثبت حدت اصلاح‌شده جدا از حدت بدون اصلاح، بدون تغییر سوابق قدیمی.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261030120000_EyeCorrectedVisualAcuity")]
public partial class EyeCorrectedVisualAcuity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "RightCorrectedVisualAcuity", table: "EyeExamRecords", type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "حدت بینایی راست با اصلاح");
        migrationBuilder.AddColumn<string>(name: "LeftCorrectedVisualAcuity", table: "EyeExamRecords", type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "حدت بینایی چپ با اصلاح");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RightCorrectedVisualAcuity", table: "EyeExamRecords");
        migrationBuilder.DropColumn(name: "LeftCorrectedVisualAcuity", table: "EyeExamRecords");
    }
}

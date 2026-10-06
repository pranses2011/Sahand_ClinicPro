using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13PrintTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrintTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "نام نمایشی قالب"),
                    Type = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع سند — ۹ نوع چاپی"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "توضیح اختیاری"),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "بدنه HTML با متغیرهای {{نام_متغیر}}"),
                    PaperSize = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "اندازه کاغذ — A4/A5/A6"),
                    Orientation = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false, comment: "جهت صفحه — Portrait/Landscape"),
                    FontSize = table.Column<int>(type: "int", nullable: false, comment: "اندازه فونت پایه (پیکسل)"),
                    FontFamily = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "خانواده فونت"),
                    ShowClinicLogo = table.Column<bool>(type: "bit", nullable: false, comment: "نمایش لوگوی درمانگاه در سربرگ"),
                    AccentColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true, comment: "رنگ تأکیدی — هگز"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "فعال"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, comment: "پیش‌فرض این نوع"),
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
                    table.PrimaryKey("PK_PrintTemplates", x => x.Id);
                },
                comment: "قالب چاپ سفارشی — طراح آنلاین با متغیرهای جایگاه‌دار");

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplate_Type_Default",
                table: "PrintTemplates",
                columns: new[] { "Type", "IsDefault" },
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintTemplates");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle31PaperSizeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaperSize",
                table: "PrintTemplates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                comment: "اندازه کاغذ — A4/A5/A6/Thermal80/Custom:WxH",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldComment: "اندازه کاغذ — A4/A5/A6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PaperSize",
                table: "PrintTemplates",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                comment: "اندازه کاغذ — A4/A5/A6",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldComment: "اندازه کاغذ — A4/A5/A6/Thermal80/Custom:WxH");
        }
    }
}

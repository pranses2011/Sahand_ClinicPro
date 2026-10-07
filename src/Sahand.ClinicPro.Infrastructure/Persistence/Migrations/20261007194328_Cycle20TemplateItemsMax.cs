using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle20TemplateItemsMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Items",
                table: "PrescriptionTemplates",
                type: "nvarchar(max)",
                nullable: true,
                comment: "اقلام قالب - JSON",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldComment: "اقلام قالب - JSON");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Items",
                table: "PrescriptionTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                comment: "اقلام قالب - JSON",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldComment: "اقلام قالب - JSON");
        }
    }
}

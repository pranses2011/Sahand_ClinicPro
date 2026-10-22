using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>افزودن گزینهٔ شمول بیمه به پروندهٔ پرسنل.</summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261022120000_AddEmployeeInsuranceFlag")]
    public partial class AddEmployeeInsuranceFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInsured",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "شمول بیمه سهم کارمند در محاسبه فیش حقوقی");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInsured",
                table: "Employees");
        }
    }
}

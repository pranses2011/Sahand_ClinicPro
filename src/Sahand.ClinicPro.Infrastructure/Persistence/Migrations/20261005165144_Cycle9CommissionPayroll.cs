using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle9CommissionPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultCost",
                table: "Services",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "هزینه تمام‌شده پایه هر واحد — مبنای سود خدمت (سود = قیمت − هزینه)");

            migrationBuilder.AddColumn<long>(
                name: "PerformedById",
                table: "InvoiceItems",
                type: "bigint",
                nullable: true,
                comment: "شناسه پرسنل انجام‌دهنده خدمت — مبنای حقوق درصدی سود خدمات انجامی همان پرسنل");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "InvoiceItems",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "عکس‌فوری هزینه تمام‌شده هر واحد در لحظه صدور — مبنای سود این قلم");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercent",
                table: "EmployeeSalaries",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "عکس‌فوری درصد حقوق از سود خدمات در لحظه محاسبه");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCommission",
                table: "EmployeeSalaries",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "حقوق درصدی دوره = سود خدمات × درصد ÷ ۱۰۰");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceProfitBase",
                table: "EmployeeSalaries",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "جمع سود خدمات انجامی خود پرسنل در دوره — مبنا (نه کل دریافتی)");

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionPercent",
                table: "Employees",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                comment: "درصد حقوق از سود خدمات انجامی خود پرسنل (نه کل دریافتی) — صفر = فقط حقوق پایه");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItem_PerformedBy",
                table: "InvoiceItems",
                column: "PerformedById");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Employees_PerformedById",
                table: "InvoiceItems",
                column: "PerformedById",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Employees_PerformedById",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItem_PerformedBy",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DefaultCost",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PerformedById",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "CommissionPercent",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "ServiceCommission",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "ServiceProfitBase",
                table: "EmployeeSalaries");

            migrationBuilder.DropColumn(
                name: "CommissionPercent",
                table: "Employees");
        }
    }
}

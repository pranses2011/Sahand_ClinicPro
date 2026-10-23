using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <summary>ذخیرهٔ عکس‌فوری بهای خرید اقلام مصرف‌شده برای محاسبهٔ سود حقوق درصدی.</summary>
    [global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext))]
    [global::Microsoft.EntityFrameworkCore.Migrations.Migration("20261022150000_PharmacyConsumptionPurchaseCost")]
    public partial class PharmacyConsumptionPurchaseCost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseUnitCost",
                table: "PharmacyTransactions",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true,
                comment: "عکس‌فوری بهای خرید واحد برای محاسبهٔ سود مصرفی‌ها");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseUnitCost",
                table: "PharmacyTransactions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>شناسه یکتای اختیاری درخواست پرداخت؛ سوابق قبلی بدون تغییر می‌مانند.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261101120000_PaymentRequestId")]
public partial class PaymentRequestId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "RequestId", table: "Payments", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Payment_RequestId", table: "Payments", column: "RequestId", unique: true, filter: "[RequestId] IS NOT NULL");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Payment_RequestId", table: "Payments");
        migrationBuilder.DropColumn(name: "RequestId", table: "Payments");
    }
}

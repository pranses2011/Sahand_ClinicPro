using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>شناسه و اثر انگشت اختیاری درخواست صدور فاکتور.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261102120000_InvoiceRequestId")]
public partial class InvoiceRequestId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "RequestId", table: "Invoices", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RequestFingerprint", table: "Invoices", type: "varchar(64)", unicode: false, maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Invoice_RequestId", table: "Invoices", column: "RequestId", unique: true, filter: "[RequestId] IS NOT NULL");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Invoice_RequestId", table: "Invoices");
        migrationBuilder.DropColumn(name: "RequestId", table: "Invoices");
        migrationBuilder.DropColumn(name: "RequestFingerprint", table: "Invoices");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>شناسه و اثر انگشت اختیاری درخواست پذیرش.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261103120000_ReceptionRequestId")]
public partial class ReceptionRequestId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "RequestId", table: "Receptions", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RequestFingerprint", table: "Receptions", type: "varchar(64)", unicode: false, maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Reception_RequestId", table: "Receptions", column: "RequestId", unique: true, filter: "[RequestId] IS NOT NULL");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Reception_RequestId", table: "Receptions");
        migrationBuilder.DropColumn(name: "RequestId", table: "Receptions");
        migrationBuilder.DropColumn(name: "RequestFingerprint", table: "Receptions");
    }
}

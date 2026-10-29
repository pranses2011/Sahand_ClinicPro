using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>ارجاع مستقل، بدون ساخت جلسه درمانی.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261031120000_PhysiotherapyReferrals")]
public partial class PhysiotherapyReferrals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "PhysiotherapyReferrals", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            VisitId = table.Column<long>(type: "bigint", nullable: false),
            PatientId = table.Column<long>(type: "bigint", nullable: false),
            DoctorId = table.Column<long>(type: "bigint", nullable: false),
            Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PhysiotherapyReferrals", x => x.Id);
            table.ForeignKey("FK_PhysiotherapyReferrals_Visits_VisitId", x => x.VisitId, "Visits", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_PhysiotherapyReferrals_Patients_PatientId", x => x.PatientId, "Patients", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_PhysiotherapyReferrals_Doctors_DoctorId", x => x.DoctorId, "Doctors", "Id", onDelete: ReferentialAction.NoAction);
        });
        migrationBuilder.CreateIndex("IX_PhysiotherapyReferrals_RequestId", "PhysiotherapyReferrals", "RequestId", unique: true);
        migrationBuilder.CreateIndex("IX_PhysiotherapyReferrals_PatientId_Id", "PhysiotherapyReferrals", new[] { "PatientId", "Id" });
        migrationBuilder.CreateIndex("IX_PhysiotherapyReferrals_VisitId", "PhysiotherapyReferrals", "VisitId");
        migrationBuilder.CreateIndex("IX_PhysiotherapyReferrals_DoctorId", "PhysiotherapyReferrals", "DoctorId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("PhysiotherapyReferrals");
}

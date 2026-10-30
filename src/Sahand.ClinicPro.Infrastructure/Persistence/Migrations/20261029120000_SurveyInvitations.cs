using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>ذخیره چکیده دعوت‌های یک‌بارمصرف بدون نگهداری هویت گیرنده.</summary>
[global::Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ClinicProDbContext))]
[Migration("20261029120000_SurveyInvitations")]
public partial class SurveyInvitations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SurveyInvitations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SurveyId = table.Column<long>(type: "bigint", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SurveyInvitations", x => x.Id);
                table.ForeignKey("FK_SurveyInvitations_Surveys_SurveyId", x => x.SurveyId, "Surveys", "Id", onDelete: ReferentialAction.NoAction);
            });
        migrationBuilder.CreateIndex("IX_SurveyInvitations_TokenHash", "SurveyInvitations", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_SurveyInvitations_SurveyId", "SurveyInvitations", "SurveyId");
        migrationBuilder.CreateIndex("IX_SurveyInvitations_ExpiresAt", "SurveyInvitations", "ExpiresAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("SurveyInvitations");
}

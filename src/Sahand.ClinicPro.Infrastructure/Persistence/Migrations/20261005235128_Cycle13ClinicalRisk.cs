using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13ClinicalRisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CausesSedation",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "هشدار خواب‌آلودگی/رانندگی");

            migrationBuilder.AddColumn<bool>(
                name: "HasAbusePotential",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "خطر وابستگی/اعتیاد");

            migrationBuilder.AddColumn<bool>(
                name: "IsLactationRisk",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "خطر مصرف در شیردهی");

            migrationBuilder.AddColumn<string>(
                name: "MaxDailyDose",
                table: "Medications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "حداکثر دوز روزانه مجاز — متن توضیحی");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsGeriatricCaution",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "احتیاط مصرف در سالمندان بالای ۶۵ سال (Beers)");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsHepaticCaution",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "احتیاط در نارسایی کبدی");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsPediatricCaution",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "احتیاط مصرف در کودکان زیر ۱۲ سال");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsRenalCaution",
                table: "Medications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "احتیاط در نارسایی کلیه — تعدیل دوز");

            migrationBuilder.AddColumn<byte>(
                name: "PregnancyCategory",
                table: "Medications",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0,
                comment: "رده بارداری FDA (۰=نرده‌بندی ۱=A … ۵=X) — هشدار تجویز در بارداری");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CausesSedation",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "HasAbusePotential",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "IsLactationRisk",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "MaxDailyDose",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NeedsGeriatricCaution",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NeedsHepaticCaution",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NeedsPediatricCaution",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "NeedsRenalCaution",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "PregnancyCategory",
                table: "Medications");
        }
    }
}

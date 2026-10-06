using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13Waitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialtyId = table.Column<long>(type: "bigint", nullable: true),
                    PreferredFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredTo = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredTimeOfDay = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, comment: "بازه روز دلخواه — صبح/عصر/شب/هر"),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false, comment: "اولویت — بالاتر اول پیشنهاد می‌شود"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت — در انتظار/اطلاع‌رسانی/تأیید/منقضی/لغو"),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfferedAppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactMobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true, comment: "شماره تماس عکس‌فوری هنگام ثبت"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                },
                comment: "لیست انتظار نوبت — بیماران منتظر جای خالی با پیشنهاد خودکار");

            migrationBuilder.CreateIndex(
                name: "IX_Waitlist_Status_Doctor",
                table: "WaitlistEntries",
                columns: new[] { "Status", "DoctorId", "SpecialtyId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_DoctorId",
                table: "WaitlistEntries",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_PatientId",
                table: "WaitlistEntries",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitlistEntries");
        }
    }
}

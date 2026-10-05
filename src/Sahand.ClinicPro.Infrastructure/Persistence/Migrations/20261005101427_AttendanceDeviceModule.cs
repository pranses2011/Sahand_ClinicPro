using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceDeviceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeAttendances_EmployeeId",
                table: "EmployeeAttendances");

            migrationBuilder.AddColumn<int>(
                name: "BreakMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "استراحت کسرشونده (دقیقه)");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Shifts",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true,
                comment: "رنگ تقویمی هگز");

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveGraceMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "تحمل تعجیل (دقیقه)");

            migrationBuilder.AddColumn<int>(
                name: "LateGraceMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "تحمل تأخیر (دقیقه)");

            migrationBuilder.AddColumn<int>(
                name: "MinMinutesBetweenPunches",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "حداقل فاصله ضربه‌ها (دقیقه)");

            migrationBuilder.AddColumn<int>(
                name: "OvertimeStartAfterMinutes",
                table: "Shifts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "شروع اضافه‌کاری پس از (دقیقه)");

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveMinutes",
                table: "EmployeeAttendances",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "دقایق تعجیل خروج");

            migrationBuilder.AddColumn<bool>(
                name: "IsManuallyEdited",
                table: "EmployeeAttendances",
                type: "bit",
                nullable: false,
                defaultValue: false,
                comment: "پرچم ویرایش دستی");

            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "EmployeeAttendances",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "دقایق تأخیر ورود");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "EmployeeAttendances",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                comment: "یادداشت");

            migrationBuilder.AddColumn<long>(
                name: "SourceDeviceId",
                table: "EmployeeAttendances",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceCorrections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "فیلد مورد اصلاح"),
                    OldValue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "مقدار فعلی"),
                    NewValue = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, comment: "مقدار درخواستی"),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "دلیل اصلاح"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت درخواست"),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "ملاحظات تأییدکننده"),
                    AppliedAttendanceId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_AttendanceCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "درخواست اصلاح کارت حضور - جریان تأیید سرپرست");

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, comment: "نام نمایشی دستگاه"),
                    Brand = table.Column<byte>(type: "tinyint", nullable: false, comment: "برند/پروتکل دستگاه"),
                    Model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true, comment: "مدل دستگاه"),
                    SerialNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true, comment: "شماره سریال"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false, comment: "نشانی IP"),
                    Port = table.Column<int>(type: "int", nullable: false),
                    CommunicationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "کلید ارتباطی"),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, comment: "محل نصب"),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت اتصال"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncLogCount = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "آخرین خطا"),
                    Notes = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true, comment: "توضیحات"),
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
                    table.PrimaryKey("PK_AttendanceDevices", x => x.Id);
                },
                comment: "دستگاه‌های حضور و غیاب - اتصال و همگام‌سازی");

            migrationBuilder.CreateTable(
                name: "EmployeeShiftAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ShiftId = table.Column<long>(type: "bigint", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Recurrence = table.Column<byte>(type: "tinyint", nullable: false, comment: "الگوی تکرار"),
                    WeekDaysMask = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "یادداشت"),
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
                    table.PrimaryKey("PK_EmployeeShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeShiftAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeShiftAssignments_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id");
                },
                comment: "تخصیص شیفت به پرسنل با الگوی تکرار");

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, comment: "عنوان تعطیلی"),
                    HolidayType = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع تعطیلی"),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    PartialStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    PartialEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    OnCallEmployeeIds = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false, comment: "شناسه‌های پرسنل پوشش‌دهنده جدا با کاما"),
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
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                },
                comment: "تقویم تعطیلات - رسمی و سازمانی");

            migrationBuilder.CreateTable(
                name: "DeviceRawLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    EnrollNumber = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, comment: "شناسه روی دستگاه"),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PunchState = table.Column<byte>(type: "tinyint", nullable: false, comment: "نوع ضربه"),
                    RawStateCode = table.Column<byte>(type: "tinyint", nullable: true),
                    VerifyMode = table.Column<byte>(type: "tinyint", nullable: false),
                    InOutPort = table.Column<byte>(type: "tinyint", nullable: true),
                    ProcessedStatus = table.Column<byte>(type: "tinyint", nullable: false, comment: "وضعیت پردازش"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendanceId = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true, comment: "خطای پردازش"),
                    SyncBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_DeviceRawLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceRawLogs_AttendanceDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "AttendanceDevices",
                        principalColumn: "Id");
                },
                comment: "لاگ خام ضربه‌ها - منبع حقیقت پیش از پردازش");

            migrationBuilder.CreateTable(
                name: "DeviceUserMappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<long>(type: "bigint", nullable: false),
                    EnrollNumber = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, comment: "شناسه روی دستگاه"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PreferredVerifyMode = table.Column<byte>(type: "tinyint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_DeviceUserMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceUserMappings_AttendanceDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "AttendanceDevices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeviceUserMappings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                },
                comment: "نگاشت شناسه کاربر دستگاه به پرسنل درمانگاه");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_Date",
                table: "EmployeeAttendances",
                column: "AttendanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_Employee_Date_Unique",
                table: "EmployeeAttendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_SourceDeviceId",
                table: "EmployeeAttendances",
                column: "SourceDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_Employee_Date",
                table: "AttendanceCorrections",
                columns: new[] { "EmployeeId", "AttendanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_Status",
                table: "AttendanceCorrections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_Ip",
                table: "AttendanceDevices",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDevices_IsActive",
                table: "AttendanceDevices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRawLogs_Status_Time",
                table: "DeviceRawLogs",
                columns: new[] { "ProcessedStatus", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRawLogs_Unique_Punch",
                table: "DeviceRawLogs",
                columns: new[] { "DeviceId", "EnrollNumber", "PunchTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserMappings_Device_Enroll",
                table: "DeviceUserMappings",
                columns: new[] { "DeviceId", "EnrollNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUserMappings_Employee",
                table: "DeviceUserMappings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Employee_From",
                table: "EmployeeShiftAssignments",
                columns: new[] { "EmployeeId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftAssignments_Shift",
                table: "EmployeeShiftAssignments",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date",
                table: "Holidays",
                column: "Date");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAttendances_AttendanceDevices_SourceDeviceId",
                table: "EmployeeAttendances",
                column: "SourceDeviceId",
                principalTable: "AttendanceDevices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAttendances_AttendanceDevices_SourceDeviceId",
                table: "EmployeeAttendances");

            migrationBuilder.DropTable(
                name: "AttendanceCorrections");

            migrationBuilder.DropTable(
                name: "DeviceRawLogs");

            migrationBuilder.DropTable(
                name: "DeviceUserMappings");

            migrationBuilder.DropTable(
                name: "EmployeeShiftAssignments");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "AttendanceDevices");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAttendances_Date",
                table: "EmployeeAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAttendances_Employee_Date_Unique",
                table: "EmployeeAttendances");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAttendances_SourceDeviceId",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveGraceMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "LateGraceMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "MinMinutesBetweenPunches",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "OvertimeStartAfterMinutes",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveMinutes",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "IsManuallyEdited",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "SourceDeviceId",
                table: "EmployeeAttendances");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_EmployeeId",
                table: "EmployeeAttendances",
                column: "EmployeeId");
        }
    }
}

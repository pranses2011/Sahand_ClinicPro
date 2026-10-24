using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations;

/// <summary>
/// چرخهٔ ۸۴ — رمز موقت بازیابی «در انتظار»: ستون‌های جدا از رمز اصلی کاربر
/// تا اگر نامهٔ بازیابی نادیده گرفته شد، رمز قبلی کاربر دست‌نخورده بماند.
/// </summary>
[DbContext(typeof(ClinicProDbContext))]
[Migration("20261024120000_PendingRecoveryPassword")]
public partial class PendingRecoveryPassword : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PendingRecoveryPasswordHash",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true,
            comment: "هش رمز موقت بازیابی در انتظار — جدا از رمز اصلی");

        migrationBuilder.AddColumn<DateTime>(
            name: "PendingRecoveryExpiresAt",
            table: "Users",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PendingRecoveryPasswordHash", table: "Users");
        migrationBuilder.DropColumn(name: "PendingRecoveryExpiresAt", table: "Users");
    }
}

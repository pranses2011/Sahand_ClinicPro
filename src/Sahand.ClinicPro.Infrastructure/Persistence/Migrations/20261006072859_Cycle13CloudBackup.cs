using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13CloudBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudFileName",
                table: "BackupHistory",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                comment: "نام فایل در مقصد ابری");

            migrationBuilder.AddColumn<string>(
                name: "CloudProvider",
                table: "BackupHistory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                comment: "نام ارائه‌دهنده ابری");

            migrationBuilder.AddColumn<DateTime>(
                name: "CloudUploadedAt",
                table: "BackupHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCloudEncrypted",
                table: "BackupHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudFileName",
                table: "BackupHistory");

            migrationBuilder.DropColumn(
                name: "CloudProvider",
                table: "BackupHistory");

            migrationBuilder.DropColumn(
                name: "CloudUploadedAt",
                table: "BackupHistory");

            migrationBuilder.DropColumn(
                name: "IsCloudEncrypted",
                table: "BackupHistory");
        }
    }
}

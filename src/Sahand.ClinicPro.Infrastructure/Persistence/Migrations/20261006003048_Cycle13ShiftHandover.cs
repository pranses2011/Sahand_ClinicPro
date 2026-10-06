using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Cycle13ShiftHandover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosedByName",
                table: "CashRegisters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "نام بستن‌دهنده شیفت — عکس‌فوری");

            migrationBuilder.AddColumn<long>(
                name: "ClosedByUserId",
                table: "CashRegisters",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CountedCard",
                table: "CashRegisters",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "شمارش فیزیکی کارت");

            migrationBuilder.AddColumn<decimal>(
                name: "CountedCash",
                table: "CashRegisters",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m,
                comment: "شمارش فیزیکی نقد صندوقدار");

            migrationBuilder.AddColumn<string>(
                name: "HandoverNote",
                table: "CashRegisters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "یادداشت تحویل شیفت");

            migrationBuilder.AddColumn<long>(
                name: "HandoverToUserId",
                table: "CashRegisters",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptNumber",
                table: "CashRegisters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                comment: "شماره رسید تحویل شیفت — SHR-####");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedByName",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "CountedCard",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "CountedCash",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "HandoverNote",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "HandoverToUserId",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "ReceiptNumber",
                table: "CashRegisters");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sahand.ClinicPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StocktakingItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "StockTakings",
                type: "datetime2",
                nullable: true,
                comment: "تاریخ تأیید و اعمال مغایرت — گام P2");

            migrationBuilder.AddColumn<string>(
                name: "TakingNumber",
                table: "StockTakings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                comment: "شماره جلسه انبارگردانی");

            migrationBuilder.CreateTable(
                name: "StockTakingItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakingId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryId = table.Column<long>(type: "bigint", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "موجودی سیستم هنگام شروع"),
                    CountedQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, comment: "مقدار شمارش‌شده"),
                    IsCounted = table.Column<bool>(type: "bit", nullable: false, comment: "آیا شمارش شده؟"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "یادداشت شمارنده"),
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
                    table.PrimaryKey("PK_StockTakingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakingItems_PharmacyInventory_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "PharmacyInventory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTakingItems_StockTakings_StockTakingId",
                        column: x => x.StockTakingId,
                        principalTable: "StockTakings",
                        principalColumn: "Id");
                },
                comment: "اقلام انبارگردانی - عکس فوری موجودی + شمارش واقعی");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingItems_InventoryId",
                table: "StockTakingItems",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingItems_StockTakingId",
                table: "StockTakingItems",
                column: "StockTakingId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakingItems_StockTakingId_InventoryId",
                table: "StockTakingItems",
                columns: new[] { "StockTakingId", "InventoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockTakingItems");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "StockTakings");

            migrationBuilder.DropColumn(
                name: "TakingNumber",
                table: "StockTakings");
        }
    }
}

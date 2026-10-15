using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;

#nullable disable

namespace S379658_HIT339_Assignment2_Library.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014120000_ImportReportingFinePayments")]
public partial class ImportReportingFinePayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinePayments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                BorrowingId = table.Column<int>(type: "int", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinePayments", x => x.Id);
                table.ForeignKey("FK_FinePayments_Borrowings_BorrowingId", x => x.BorrowingId,
                    "Borrowings", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_FinePayments_BorrowingId", table: "FinePayments", column: "BorrowingId");
        migrationBuilder.CreateIndex(name: "IX_FinePayments_PaidAtUtc", table: "FinePayments", column: "PaidAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FinePayments");
    }
}

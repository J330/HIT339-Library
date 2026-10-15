using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;

#nullable disable
namespace S379658_HIT339_Assignment2_Library.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014220000_CalendarScheduling")]
public partial class CalendarScheduling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CalendarEntries",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Kind = table.Column<int>(type: "int", nullable: false),
                ScheduledDate = table.Column<DateTime>(type: "date", nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                BorrowerId = table.Column<int>(type: "int", nullable: true),
                ReservationId = table.Column<int>(type: "int", nullable: true),
                Delivery = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CalendarEntries", x => x.Id);
                table.ForeignKey("FK_CalendarEntries_Borrowers_BorrowerId", x => x.BorrowerId,
                    "Borrowers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_CalendarEntries_Reservations_ReservationId", x => x.ReservationId,
                    "Reservations", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_CalendarEntries_ScheduledDate_Kind", table: "CalendarEntries",
            columns: new[] { "ScheduledDate", "Kind" });
        migrationBuilder.CreateIndex(name: "IX_CalendarEntries_BorrowerId_ScheduledDate", table: "CalendarEntries",
            columns: new[] { "BorrowerId", "ScheduledDate" });
        migrationBuilder.CreateIndex(name: "IX_CalendarEntries_ReservationId", table: "CalendarEntries",
            column: "ReservationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CalendarEntries");
    }
}

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using S379658_HIT339_Assignment2_Library.Database;
#nullable disable
namespace S379658_HIT339_Assignment2_Library.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261014060000_BranchesReservationsNotifications")]
public partial class BranchesReservationsNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "LibraryBranches", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
            Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            OpeningHours = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_LibraryBranches", x => x.Id));
        // Backfill pre-existing inventory/loans to Main Library while retaining all records.
        // Explicit store types are required because this hand-authored migration
        // has no generated TargetModel containing LibraryBranches metadata.
        migrationBuilder.InsertData(
            table: "LibraryBranches",
            columns: new[] { "Id", "Name", "Code", "Address", "Phone", "OpeningHours", "IsActive" },
            columnTypes: new[] { "int", "nvarchar(100)", "nvarchar(16)", "nvarchar(200)", "nvarchar(40)", "nvarchar(150)", "bit" },
            values: new object[] { 1, "Main Library", "MAIN", "Set main library address", "", "Mon–Fri 09:00–17:00", true });
        migrationBuilder.CreateIndex(name: "IX_LibraryBranches_Code", table: "LibraryBranches", column: "Code", unique: true);
        migrationBuilder.AddColumn<int>(name: "BranchId", table: "Items", type: "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<int>(name: "CheckoutBranchId", table: "Borrowings", type: "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<int>(name: "ReturnBranchId", table: "Borrowings", type: "int", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Items_BranchId", table: "Items", column: "BranchId");
        migrationBuilder.CreateIndex(name: "IX_Borrowings_CheckoutBranchId", table: "Borrowings", column: "CheckoutBranchId");
        migrationBuilder.CreateIndex(name: "IX_Borrowings_ReturnBranchId", table: "Borrowings", column: "ReturnBranchId");
        migrationBuilder.AddForeignKey(name: "FK_Items_LibraryBranches_BranchId", table: "Items",
            column: "BranchId", principalTable: "LibraryBranches", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Borrowings_LibraryBranches_CheckoutBranchId", table: "Borrowings",
            column: "CheckoutBranchId", principalTable: "LibraryBranches", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_Borrowings_LibraryBranches_ReturnBranchId", table: "Borrowings",
            column: "ReturnBranchId", principalTable: "LibraryBranches", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.CreateTable(name: "ReceptionDesks", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
            BranchId = table.Column<int>(type: "int", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_ReceptionDesks", x => x.Id);
            table.ForeignKey("FK_ReceptionDesks_LibraryBranches_BranchId", x => x.BranchId, "LibraryBranches", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_ReceptionDesks_BranchId", table: "ReceptionDesks", column: "BranchId");
        migrationBuilder.CreateTable(name: "ItemTransfers", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ItemId = table.Column<int>(type: "int", nullable: false),
            SourceBranchId = table.Column<int>(type: "int", nullable: false),
            DestinationBranchId = table.Column<int>(type: "int", nullable: false),
            Status = table.Column<int>(type: "int", nullable: false),
            RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_ItemTransfers", x => x.Id);
            table.ForeignKey("FK_ItemTransfers_Items_ItemId", x => x.ItemId, "Items", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ItemTransfers_LibraryBranches_SourceBranchId", x => x.SourceBranchId, "LibraryBranches", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_ItemTransfers_LibraryBranches_DestinationBranchId", x => x.DestinationBranchId, "LibraryBranches", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_ItemTransfers_ItemId", table: "ItemTransfers", column: "ItemId", unique: true, filter: "[Status] = 0");
        migrationBuilder.CreateIndex(name: "IX_ItemTransfers_SourceBranchId", table: "ItemTransfers", column: "SourceBranchId");
        migrationBuilder.CreateIndex(name: "IX_ItemTransfers_DestinationBranchId", table: "ItemTransfers", column: "DestinationBranchId");
        migrationBuilder.CreateTable(name: "Reservations", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ItemId = table.Column<int>(type: "int", nullable: false),
            BorrowerId = table.Column<int>(type: "int", nullable: false),
            Status = table.Column<int>(type: "int", nullable: false),
            PlacedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            ReadyAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
            CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table => {
            table.PrimaryKey("PK_Reservations", x => x.Id);
            table.ForeignKey("FK_Reservations_Items_ItemId", x => x.ItemId, "Items", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_Reservations_Borrowers_BorrowerId", x => x.BorrowerId, "Borrowers", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_Reservations_ItemId_BorrowerId", table: "Reservations",
            columns: new[] { "ItemId", "BorrowerId" }, unique: true, filter: "[Status] IN (0, 1)");
        migrationBuilder.CreateIndex(name: "IX_Reservations_ItemId_Status_PlacedAtUtc_Id", table: "Reservations",
            columns: new[] { "ItemId", "Status", "PlacedAtUtc", "Id" });
        migrationBuilder.CreateIndex(name: "IX_Reservations_BorrowerId", table: "Reservations", column: "BorrowerId");
        migrationBuilder.CreateTable(name: "LibraryNotifications", columns: table => new
        {
            Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            BorrowerId = table.Column<int>(type: "int", nullable: false),
            Channel = table.Column<int>(type: "int", nullable: false),
            Type = table.Column<int>(type: "int", nullable: false),
            Recipient = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
            Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
            Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            ReferenceKey = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            DeliveryStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_LibraryNotifications", x => x.Id);
            table.ForeignKey("FK_LibraryNotifications_Borrowers_BorrowerId", x => x.BorrowerId, "Borrowers", "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex(name: "IX_LibraryNotifications_BorrowerId", table: "LibraryNotifications", column: "BorrowerId");
        migrationBuilder.CreateIndex(name: "IX_LibraryNotifications_ReferenceKey", table: "LibraryNotifications", column: "ReferenceKey", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LibraryNotifications");
        migrationBuilder.DropTable(name: "Reservations");
        migrationBuilder.DropTable(name: "ItemTransfers");
        migrationBuilder.DropTable(name: "ReceptionDesks");
        migrationBuilder.DropForeignKey(name: "FK_Items_LibraryBranches_BranchId", table: "Items");
        migrationBuilder.DropForeignKey(name: "FK_Borrowings_LibraryBranches_CheckoutBranchId", table: "Borrowings");
        migrationBuilder.DropForeignKey(name: "FK_Borrowings_LibraryBranches_ReturnBranchId", table: "Borrowings");
        migrationBuilder.DropIndex(name: "IX_Items_BranchId", table: "Items");
        migrationBuilder.DropIndex(name: "IX_Borrowings_CheckoutBranchId", table: "Borrowings");
        migrationBuilder.DropIndex(name: "IX_Borrowings_ReturnBranchId", table: "Borrowings");
        migrationBuilder.DropColumn(name: "BranchId", table: "Items");
        migrationBuilder.DropColumn(name: "CheckoutBranchId", table: "Borrowings");
        migrationBuilder.DropColumn(name: "ReturnBranchId", table: "Borrowings");
        migrationBuilder.DropTable(name: "LibraryBranches");
    }
}

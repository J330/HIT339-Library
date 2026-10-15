using System.Globalization;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Services;

public sealed class ManagerReportService
{
    private readonly AppDbContext _db;
    public ManagerReportService(AppDbContext db) => _db = db;
    private static string Money(decimal d) => d.ToString("0.00", CultureInfo.InvariantCulture);
    private static IQueryable<Borrowing> FilterDates(IQueryable<Borrowing> q, DateTime? from, DateTime? to)
    {
        if (from.HasValue) q = q.Where(b => b.BorrowDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(b => b.BorrowDate < to.Value.Date.AddDays(1));
        return q;
    }

    public async Task<ExtendedManagerDashboard> OverviewAsync()
    {
        var branches = await _db.LibraryBranches.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        var inventory = await _db.Items.AsNoTracking().GroupBy(i => i.BranchId)
            .Select(g => new { BranchId = g.Key, Total = g.Count(), Available = g.Count(i => i.Status == ItemStatus.Available),
                Bad = g.Count(i => i.Status == ItemStatus.Damaged || i.Status == ItemStatus.Destroyed) }).ToListAsync();
        var loans = await _db.Borrowings.AsNoTracking().Where(b => b.ReturnDate == null)
            .GroupBy(b => b.CheckoutBranchId).Select(g => new { BranchId = g.Key, Count = g.Count() }).ToListAsync();
        var reservations = await _db.Reservations.AsNoTracking().GroupBy(r => r.Item.BranchId)
            .Select(g => new { BranchId = g.Key, Pending = g.Count(r => r.Status == ReservationStatus.Pending),
                Ready = g.Count(r => r.Status == ReservationStatus.Ready) }).ToListAsync();
        var transfers = await _db.ItemTransfers.AsNoTracking().Where(t => t.Status == TransferStatus.Pending)
            .GroupBy(t => t.SourceBranchId).Select(g => new { BranchId = g.Key, Count = g.Count() }).ToListAsync();
        var model = new ExtendedManagerDashboard
        {
            TotalFinesCharged = await _db.Borrowings.AsNoTracking().SumAsync(b => (decimal?)b.FineAmount) ?? 0,
            TotalPaymentsReceived = await _db.FinePayments.AsNoTracking().SumAsync(p => (decimal?)p.Amount) ?? 0
        };
        foreach (var b in branches)
        {
            var i = inventory.FirstOrDefault(x => x.BranchId == b.Id);
            var r = reservations.FirstOrDefault(x => x.BranchId == b.Id);
            model.Branches.Add(new BranchSummaryRow
            {
                BranchId = b.Id, BranchName = b.Name, Copies = i?.Total ?? 0,
                Available = i?.Available ?? 0, DamagedOrDestroyed = i?.Bad ?? 0,
                ActiveLoans = loans.FirstOrDefault(x => x.BranchId == b.Id)?.Count ?? 0,
                PendingReservations = r?.Pending ?? 0, ReadyReservations = r?.Ready ?? 0,
                PendingTransfers = transfers.FirstOrDefault(x => x.BranchId == b.Id)?.Count ?? 0
            });
        }
        return model;
    }

    public async Task<ReportTable> BuildReportAsync(ManagerReportType type, int? branchId, DateTime? from, DateTime? to)
    {
        var table = new ReportTable { Title = $"{type} report", Note = "All dates and payment timestamps are reported as stored; payment dates use UTC." };
        var branches = await _db.LibraryBranches.AsNoTracking().OrderBy(b => b.Name)
            .Where(b => !branchId.HasValue || b.Id == branchId).ToListAsync();
        if (type == ManagerReportType.Inventory)
        {
            var summary = await OverviewAsync();
            table.Note = "Current inventory and queue snapshot. Date filters do not apply to inventory.";
            table.Columns = ["Branch", "Copies", "Available", "Damaged/destroyed", "Active loans", "Queued holds", "Ready holds", "Pending transfers"];
            foreach (var b in summary.Branches.Where(b => !branchId.HasValue || b.BranchId == branchId))
                table.Rows.Add([b.BranchName, b.Copies.ToString(), b.Available.ToString(), b.DamagedOrDestroyed.ToString(),
                    b.ActiveLoans.ToString(), b.PendingReservations.ToString(), b.ReadyReservations.ToString(), b.PendingTransfers.ToString()]);
        }
        else if (type == ManagerReportType.Borrowing)
        {
            var q = FilterDates(_db.Borrowings.AsNoTracking(), from, to);
            var groups = await q.GroupBy(b => b.CheckoutBranchId)
                .Select(g => new { BranchId = g.Key, Total = g.Count(), Active = g.Count(b => b.ReturnDate == null),
                    Returned = g.Count(b => b.ReturnDate != null),
                    Overdue = g.Count(b => b.ReturnDate == null && b.DueDate < DateTime.Today),
                    Late = g.Count(b => b.ReturnDate != null && b.FineAmount > 0) }).ToListAsync();
            table.Columns = ["Checkout branch", "Total loans", "Active", "Returned", "Overdue now", "Returned late"];
            foreach (var b in branches)
            {
                var g = groups.FirstOrDefault(x => x.BranchId == b.Id);
                table.Rows.Add([b.Name, (g?.Total ?? 0).ToString(), (g?.Active ?? 0).ToString(),
                    (g?.Returned ?? 0).ToString(), (g?.Overdue ?? 0).ToString(), (g?.Late ?? 0).ToString()]);
            }
        }
        else if (type == ManagerReportType.Fines)
        {
            var loans = _db.Borrowings.AsNoTracking().Where(b => b.ReturnDate != null);
            if (from.HasValue) loans = loans.Where(b => b.ReturnDate >= from.Value.Date);
            if (to.HasValue) loans = loans.Where(b => b.ReturnDate < to.Value.Date.AddDays(1));
            var charges = await loans.GroupBy(b => b.CheckoutBranchId)
                .Select(g => new { BranchId = g.Key, Charged = g.Sum(b => b.FineAmount), Count = g.Count(b => b.FineAmount > 0) }).ToListAsync();
            var payments = _db.FinePayments.AsNoTracking().AsQueryable();
            if (from.HasValue) payments = payments.Where(p => p.PaidAtUtc >= from.Value.Date);
            if (to.HasValue) payments = payments.Where(p => p.PaidAtUtc < to.Value.Date.AddDays(1));
            var collected = await payments.GroupBy(p => p.Borrowing.CheckoutBranchId)
                .Select(g => new { BranchId = g.Key, Total = g.Sum(p => p.Amount) }).ToListAsync();
            table.Note = (from.HasValue || to.HasValue)
                ? "Charges are grouped by RETURN date; payments by payment UTC date. Period difference is not a running balance."
                : "Lifetime charged less lifetime payments is the current outstanding fine balance.";
            table.Columns = ["Checkout branch", "Charged (AUD)", "Payments received (AUD)", "Difference (AUD)", "Fined returns"];
            foreach (var b in branches)
            {
                var c = charges.FirstOrDefault(x => x.BranchId == b.Id);
                var paid = collected.FirstOrDefault(x => x.BranchId == b.Id)?.Total ?? 0;
                var charged = c?.Charged ?? 0;
                table.Rows.Add([b.Name, Money(charged), Money(paid), Money(charged - paid), (c?.Count ?? 0).ToString()]);
            }
        }
        else if (type == ManagerReportType.PaymentAudit)
        {
            table.Note = "Individual recorded receipts; amounts represent money received, not newly charged fines. Dates are UTC.";
            table.Columns = ["Paid UTC", "Receipt reference", "Borrowing #", "Title", "Checkout branch", "Amount (AUD)"];
            var payments = _db.FinePayments.AsNoTracking().AsQueryable();
            if (branchId.HasValue) payments = payments.Where(p => p.Borrowing.CheckoutBranchId == branchId.Value);
            if (from.HasValue) payments = payments.Where(p => p.PaidAtUtc >= from.Value.Date);
            if (to.HasValue) payments = payments.Where(p => p.PaidAtUtc < to.Value.Date.AddDays(1));
            var rows = await payments.OrderBy(p => p.PaidAtUtc).ThenBy(p => p.Id)
                .Select(p => new { p.PaidAtUtc, p.Reference, p.BorrowingId,
                    Item = p.Borrowing.Item.Name, Branch = p.Borrowing.CheckoutBranch.Name, p.Amount }).ToListAsync();
            foreach (var r in rows)
                table.Rows.Add([r.PaidAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), r.Reference,
                    r.BorrowingId.ToString(), r.Item, r.Branch, Money(r.Amount)]);
        }
        return table;
    }
}

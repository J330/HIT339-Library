using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Services;

public sealed class ManagerStatisticsService
{
    private readonly AppDbContext _context;

    public ManagerStatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BorrowingStatisticsViewModel> BuildBorrowingStatisticsAsync(DateTime today)
    {
        var date = today.Date;

        return new BorrowingStatisticsViewModel
        {
            TotalBorrowings = await _context.Borrowings.AsNoTracking().CountAsync(),
            ActiveBorrowings = await _context.Borrowings.AsNoTracking().CountAsync(b => b.ReturnDate == null),
            ReturnedBorrowings = await _context.Borrowings.AsNoTracking().CountAsync(b => b.ReturnDate != null),
            OverdueBorrowings = await _context.Borrowings.AsNoTracking()
                .CountAsync(b => b.ReturnDate == null && b.DueDate < date),
            OnTimeReturns = await _context.Borrowings.AsNoTracking()
                .CountAsync(b => b.ReturnDate != null && b.FineAmount == 0),
            LateReturns = await _context.Borrowings.AsNoTracking()
                .CountAsync(b => b.ReturnDate != null && b.FineAmount > 0)
        };
    }

    public async Task<ItemStatisticsViewModel> BuildItemStatisticsAsync()
    {
        return new ItemStatisticsViewModel
        {
            TotalItems = await _context.Items.AsNoTracking().CountAsync(),
            AvailableItems = await _context.Items.AsNoTracking().CountAsync(i => i.Status == ItemStatus.Available),
            BorrowedItems = await _context.Items.AsNoTracking().CountAsync(i => i.Status == ItemStatus.Borrowed),
            DamagedItems = await _context.Items.AsNoTracking().CountAsync(i => i.Status == ItemStatus.Damaged),
            DestroyedItems = await _context.Items.AsNoTracking().CountAsync(i => i.Status == ItemStatus.Destroyed),
            Books = await _context.Books.AsNoTracking().CountAsync(),
            Music = await _context.Music.AsNoTracking().CountAsync(),
            Toys = await _context.Toys.AsNoTracking().CountAsync()
        };
    }

    public async Task<FineStatisticsViewModel> BuildFineStatisticsAsync(DateTime today)
    {
        var date = today.Date;
        var finedQuery = _context.Borrowings.AsNoTracking().Where(b => b.FineAmount > 0);
        var overdueQuery = _context.Borrowings.AsNoTracking()
            .Where(b => b.ReturnDate == null && b.DueDate < date);

        var totalFines = await finedQuery.SumAsync(b => (decimal?)b.FineAmount) ?? 0m;
        var fineCount = await finedQuery.CountAsync();
        var averageFine = await finedQuery.AverageAsync(b => (decimal?)b.FineAmount) ?? 0m;
        var highestFine = await finedQuery.MaxAsync(b => (decimal?)b.FineAmount) ?? 0m;
        var overdueCount = await overdueQuery.CountAsync();

        // Only overdue due-dates are brought into memory; all aggregate counts/sums above stay in SQL.
        var overdueDueDates = await overdueQuery.Select(b => b.DueDate).ToListAsync();
        var estimatedCurrentOverdueFines = overdueDueDates.Sum(dueDate =>
        {
            var daysLate = (date - dueDate.Date).Days;
            return daysLate * FineService.DailyFineRate;
        });

        return new FineStatisticsViewModel
        {
            TotalFinesCharged = totalFines,
            TotalPaymentsReceived = await _context.FinePayments.AsNoTracking().SumAsync(p => (decimal?)p.Amount) ?? 0m,
            BorrowingsWithFines = fineCount,
            AverageFine = averageFine,
            HighestFine = highestFine,
            EstimatedCurrentOverdueFines = estimatedCurrentOverdueFines,
            CurrentOverdueItems = overdueCount
        };
    }
}

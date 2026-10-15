using System.Data;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
public sealed class NotificationSweepService
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;
    private readonly ReservationService _reservations;
    public NotificationSweepService(AppDbContext db, NotificationService notifications, ReservationService reservations)
    { _db = db; _notifications = notifications; _reservations = reservations; }
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var expiredHolds = await _db.Reservations.Include(r => r.Borrower).Include(r => r.Item)
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresAtUtc <= now)
            .ToListAsync(ct);
        foreach (var r in expiredHolds)
        {
            r.Status = ReservationStatus.Expired;
            await _notifications.QueueOnceAsync(r.Borrower, NotificationType.HoldExpired,
                "Your library hold has expired", $"The collection deadline for {r.Item.Name} has passed.", $"hold-expired:{r.Id}", ct);
            await _db.SaveChangesAsync(ct);
            await _reservations.PromoteNextAsync(r.Item);
        }
        var loans = await _db.Borrowings.Include(b => b.Borrower).Include(b => b.Item)
            .Where(b => b.ReturnDate == null && b.DueDate.Date <= today.AddDays(2))
            .ToListAsync(ct);
        foreach (var loan in loans)
        {
            var days = (today - loan.DueDate.Date).Days;
            if (days < 0 || days == 0)
                await _notifications.QueueOnceAsync(loan.Borrower, NotificationType.DueSoon,
                    "Library item due soon", $"{loan.Item.Name} is due on {loan.DueDate:dd MMM yyyy}.", $"due-soon:{loan.Id}", ct);
            else
            {
                await _notifications.QueueOnceAsync(loan.Borrower, NotificationType.Overdue,
                    "Library item overdue", $"{loan.Item.Name} was due {loan.DueDate:dd MMM yyyy}.", $"overdue:{loan.Id}", ct);
                await _notifications.QueueOnceAsync(loan.Borrower, NotificationType.Fine,
                    "Estimated overdue fine", $"{loan.Item.Name} is {days} day(s) late; estimated fine: {days * FineService.DailyFineRate:C}.",
                    $"fine:{loan.Id}:{today:yyyyMMdd}", ct);
            }
        }
        // Calendar notifications are due on the selected local day; they are simulated and logged.
        // Reference keys prevent duplicate SMS/email log entries if a sweep is retried.
        var planned = await _db.CalendarEntries.Include(e => e.Borrower)
            .Where(e => e.Kind == CalendarEntryKind.Notification && e.DispatchedAtUtc == null &&
                e.ScheduledDate <= today)
            .OrderBy(e => e.ScheduledDate).ThenBy(e => e.Id).ToListAsync(ct);
        foreach (var entry in planned)
        {
            if (entry.Borrower is null) continue;
            await _notifications.QueueChannelsOnceAsync(entry.Borrower, NotificationType.Scheduled,
                entry.Title, entry.Description, $"calendar:{entry.Id}",
                entry.Delivery != CalendarDelivery.Sms,
                entry.Delivery != CalendarDelivery.Email, ct);
            entry.DispatchedAtUtc = now;
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return expiredHolds.Count + loans.Count + planned.Count;
    }
}

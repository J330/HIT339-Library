using System.Data;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
public sealed class ReservationService
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;
    public ReservationService(AppDbContext db, NotificationService notifications)
    { _db = db; _notifications = notifications; }

    // Must be called in the same serializable transaction as changes to item availability.
    public async Task<Reservation?> PromoteNextAsync(Item item)
    {
        if (item.Status != ItemStatus.Available ||
            await _db.Borrowings.AnyAsync(b => b.ItemId == item.Id && b.ReturnDate == null) ||
            await _db.ItemTransfers.AnyAsync(t => t.ItemId == item.Id && t.Status == TransferStatus.Pending))
            return null;
        var alreadyReady = await _db.Reservations.AnyAsync(r =>
            r.ItemId == item.Id && r.Status == ReservationStatus.Ready);
        if (alreadyReady) return null;
        var next = await _db.Reservations.Include(r => r.Borrower)
            .Where(r => r.ItemId == item.Id && r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.PlacedAtUtc).ThenBy(r => r.Id).FirstOrDefaultAsync();
        if (next is null) return null;
        next.Status = ReservationStatus.Ready;
        next.ReadyAtUtc = DateTime.UtcNow;
        next.ExpiresAtUtc = DateTime.UtcNow.AddDays(3);
        _notifications.Queue(next.Borrower, NotificationType.HoldReady,
            "Your library hold is ready", $"{item.Name} is available to collect at your library. Hold expires {next.ExpiresAtUtc:dd MMM yyyy} (UTC).",
            $"hold-ready:{next.Id}");
        return next;
    }

    public async Task<(bool Ok, string Message)> PlaceAsync(int itemId, int borrowerId, DateTime? followUpDate = null, Guid? createdByUserId = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (followUpDate.HasValue && (followUpDate.Value.Date < DateTime.Today ||
            followUpDate.Value.Date > DateTime.Today.AddYears(2)))
            return (false, "Please select a follow-up date within the next two years.");
        var item = await _db.Items.FindAsync(itemId);
        var borrower = await _db.Borrowers.FindAsync(borrowerId);
        if (item == null || borrower == null) return (false, "Item or borrower not found.");
        if (item.Status == ItemStatus.Destroyed) return (false, "Destroyed items cannot be reserved.");
        if (await _db.Reservations.AnyAsync(r => r.ItemId == itemId && r.BorrowerId == borrowerId &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Ready)))
            return (false, "You already have a hold on this copy.");
        if (await _db.Borrowings.AnyAsync(b => b.ItemId == itemId && b.BorrowerId == borrowerId && b.ReturnDate == null))
            return (false, "You already have this copy on loan.");
        var otherHolds = await _db.Reservations.AnyAsync(r => r.ItemId == itemId &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Ready));
        if (item.Status == ItemStatus.Available && !otherHolds &&
            !await _db.ItemTransfers.AnyAsync(t => t.ItemId == itemId && t.Status == TransferStatus.Pending))
            return (false, "This copy is available now. Please check it out instead.");
        var hold = new Reservation { ItemId = itemId, BorrowerId = borrowerId,
            Status = ReservationStatus.Pending, PlacedAtUtc = DateTime.UtcNow };
        _db.Reservations.Add(hold);
        await _db.SaveChangesAsync();
        if (followUpDate.HasValue)
        {
            // Placing a hold is immediate. The chosen date is a reminder, NOT a guaranteed pickup date.
            _db.CalendarEntries.Add(new CalendarEntry
            {
                Kind = CalendarEntryKind.ReservationReminder,
                ScheduledDate = followUpDate.Value.Date,
                Title = "Check reservation: " + item.Name,
                Description = "Follow up on your hold status. Actual collection depends on availability and queue order.",
                BorrowerId = borrowerId, ReservationId = hold.Id,
                CreatedByUserId = createdByUserId ?? Guid.Empty
            });
        }
        await PromoteNextAsync(item);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, "Your reservation was added to the first-come, first-served queue.");
    }

    public async Task<(bool Ok, string Message)> CancelAsync(int reservationId, int? ownerId, bool staff)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var hold = await _db.Reservations.Include(r => r.Item).FirstOrDefaultAsync(r => r.Id == reservationId);
        if (hold == null || (!staff && hold.BorrowerId != ownerId)) return (false, "Reservation not found.");
        if (hold.Status is not (ReservationStatus.Pending or ReservationStatus.Ready))
            return (false, "This reservation is no longer active.");
        hold.Status = ReservationStatus.Cancelled;
        var reminders = await _db.CalendarEntries.Where(e => e.ReservationId == hold.Id &&
            e.Kind == CalendarEntryKind.ReservationReminder).ToListAsync();
        _db.CalendarEntries.RemoveRange(reminders);
        await _db.SaveChangesAsync();
        await PromoteNextAsync(hold.Item);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return (true, "Reservation cancelled.");
    }
}

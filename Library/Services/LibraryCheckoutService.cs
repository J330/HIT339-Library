using System.Data;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
// One transactional checkout implementation used by reception and the kiosk.
public sealed class LibraryCheckoutService
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;
    private readonly ReservationService _reservations;
    public LibraryCheckoutService(AppDbContext db, NotificationService notifications, ReservationService reservations)
    { _db = db; _notifications = notifications; _reservations = reservations; }

    public async Task<CheckoutResult> CheckoutAsync(int borrowerId, string libraryCode,
        DateTime borrowed, DateTime due, int? receptionBranchId = null,
        CheckoutChannel channel = CheckoutChannel.Reception)
    {
        libraryCode = libraryCode?.Trim() ?? "";
        if (libraryCode.Length is 0 or > 50) return new(false, "Enter a valid item library code.");
        if (due.Date <= borrowed.Date) return new(false, "Due date must follow borrowing date.");
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _db.Items.FirstOrDefaultAsync(i => i.LibraryCode == libraryCode);
        var borrower = await _db.Borrowers.FindAsync(borrowerId);
        if (item == null || borrower == null) return new(false, "Item or borrower could not be found.");
        if (receptionBranchId.HasValue && receptionBranchId != item.BranchId)
            return new(false, "This copy is currently located at a different branch.");
        if (!await _db.LibraryBranches.AnyAsync(b => b.Id == item.BranchId && b.IsActive))
            return new(false, "This branch is not accepting checkouts.");
        if (item.Status != ItemStatus.Available ||
            await _db.Borrowings.AnyAsync(b => b.ItemId == item.Id && b.ReturnDate == null) ||
            await _db.ItemTransfers.AnyAsync(t => t.ItemId == item.Id && t.Status == TransferStatus.Pending))
            return new(false, "This item is unavailable or currently being transferred.");

        // Clear expired ready holds before allowing a new checkout (even if the worker hasn't run).
        var expired = await _db.Reservations.Include(r => r.Borrower)
            .Where(r => r.ItemId == item.Id && r.Status == ReservationStatus.Ready &&
                r.ExpiresAtUtc <= DateTime.UtcNow).ToListAsync();
        foreach (var hold in expired)
        {
            hold.Status = ReservationStatus.Expired;
            _notifications.Queue(hold.Borrower, NotificationType.HoldExpired,
                "Your library hold has expired", $"The pickup window for {item.Name} has expired.",
                $"hold-expired:{hold.Id}");
        }
        await _db.SaveChangesAsync();
        // If a returned copy has queued holds, the first borrower must get first access.
        await _reservations.PromoteNextAsync(item);
        await _db.SaveChangesAsync();
        var active = await _db.Reservations.Where(r => r.ItemId == item.Id &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Ready))
            .OrderBy(r => r.PlacedAtUtc).ThenBy(r => r.Id).FirstOrDefaultAsync();
        if (active != null && (active.BorrowerId != borrowerId || active.Status != ReservationStatus.Ready))
            return new(false, "This copy is reserved for another borrower.");
        if (active != null) { active.Status = ReservationStatus.Collected; active.CollectedAtUtc = DateTime.UtcNow; }

        item.Status = ItemStatus.Borrowed;
        _db.Borrowings.Add(new Borrowing { ItemId = item.Id, BorrowerId = borrowerId,
            CheckoutBranchId = item.BranchId, BorrowDate = borrowed.Date,
            DueDate = due.Date, FineAmount = 0m, Channel = channel });
        _notifications.Queue(borrower, NotificationType.Borrowed, "Library checkout confirmation",
            $"You borrowed {item.Name} from the library. Due {due:dd MMM yyyy}.",
            "checkout:" + Guid.NewGuid().ToString("N"));
        try {
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException) { _db.ChangeTracker.Clear(); return new(false, "Checkout failed; refresh and try again."); }
        return new(true, $"Checked out {item.Name}. Due {due:dd MMM yyyy}.");
    }
}

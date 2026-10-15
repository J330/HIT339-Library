using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.Models;

public enum CalendarEntryKind { Event, Notification, ReservationReminder }
public enum CalendarDelivery { Both, Email, Sms }

// A role-based audience applies only to manually created calendar events.
// An event can be shared with any combination of staff roles and borrowers.
// Borrower audience + an empty Recipients list means every borrower may view it.
// One or more Recipients restricts the borrower audience to only those members.
// BorrowerId remains for notifications, reservation reminders, and legacy private events.
[Flags]
public enum CalendarAudience
{
    None = 0,
    Admin = 1,
    Manager = 2,
    Reception = 4,
    Borrower = 8
}

// Events and scheduled simulated messages share one calendar table.
// Notifications and reservation follow-ups remain private to their borrower.
public sealed class CalendarEntry
{
    public int Id { get; set; }
    public CalendarEntryKind Kind { get; set; }
    [DataType(DataType.Date)]
    public DateTime ScheduledDate { get; set; }
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [StringLength(800)]
    public string Description { get; set; } = "";
    public CalendarAudience Audience { get; set; } = CalendarAudience.None;
    public int? BorrowerId { get; set; }
    public Borrower? Borrower { get; set; }
    public ICollection<CalendarEntryRecipient> Recipients { get; set; } = new List<CalendarEntryRecipient>();
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public CalendarDelivery Delivery { get; set; } = CalendarDelivery.Both;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
}

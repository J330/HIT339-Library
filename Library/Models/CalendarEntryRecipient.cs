namespace S379658_HIT339_Assignment2_Library.Models;

// A borrower specifically invited to a calendar event. Empty recipient list +
// Borrower audience means the event is visible to all borrowers.
public sealed class CalendarEntryRecipient
{
    public int CalendarEntryId { get; set; }
    public CalendarEntry CalendarEntry { get; set; } = null!;
    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;
}

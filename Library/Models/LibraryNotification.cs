namespace S379658_HIT339_Assignment2_Library.Models;
public enum NotificationChannel { Email, Sms }
public enum NotificationType { Borrowed, DueSoon, Overdue, Fine, HoldReady, HoldExpired, Scheduled }
public class LibraryNotification
{
    public int Id { get; set; }
    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string ReferenceKey { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string DeliveryStatus { get; set; } = "Simulated";
}

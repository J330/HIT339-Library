using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
namespace S379658_HIT339_Assignment2_Library.Services;
// Simulator only. Stores what would be sent by email/SMS; it never contacts external gateways.
public sealed class NotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public void Queue(Borrower borrower, NotificationType type, string subject, string message, string key)
    {
        if (!string.IsNullOrWhiteSpace(borrower.Email))
            Add(borrower, NotificationChannel.Email, borrower.Email, type, subject, message, key + ":email");
        if (!string.IsNullOrWhiteSpace(borrower.Phone))
            Add(borrower, NotificationChannel.Sms, borrower.Phone, type, subject, message, key + ":sms");
    }

    private void Add(Borrower borrower, NotificationChannel channel, string recipient,
        NotificationType type, string subject, string message, string key)
    {
        // Caller commits notifications together with the event using the same EF transaction.
        _db.LibraryNotifications.Add(new LibraryNotification
        {
            BorrowerId = borrower.Id, Channel = channel, Recipient = recipient,
            Type = type, Subject = subject, Message = message, ReferenceKey = key,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    public async Task QueueChannelsOnceAsync(Borrower borrower, NotificationType type,
        string subject, string message, string key, bool sendEmail, bool sendSms,
        CancellationToken ct = default)
    {
        var keys = await _db.LibraryNotifications.AsNoTracking()
            .Where(n => n.ReferenceKey == key + ":email" || n.ReferenceKey == key + ":sms")
            .Select(n => n.ReferenceKey).ToListAsync(ct);
        if (sendEmail && !string.IsNullOrWhiteSpace(borrower.Email) && !keys.Contains(key + ":email"))
            Add(borrower, NotificationChannel.Email, borrower.Email, type, subject, message, key + ":email");
        if (sendSms && !string.IsNullOrWhiteSpace(borrower.Phone) && !keys.Contains(key + ":sms"))
            Add(borrower, NotificationChannel.Sms, borrower.Phone, type, subject, message, key + ":sms");
    }

    public async Task QueueOnceAsync(Borrower borrower, NotificationType type, string subject,
        string message, string eventKey, CancellationToken cancellationToken = default)
    {
        var existingKeys = await _db.LibraryNotifications.AsNoTracking()
            .Where(n => n.ReferenceKey == eventKey + ":email" || n.ReferenceKey == eventKey + ":sms")
            .Select(n => n.ReferenceKey).ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(borrower.Email) && !existingKeys.Contains(eventKey + ":email"))
            Add(borrower, NotificationChannel.Email, borrower.Email, type, subject, message, eventKey + ":email");
        if (!string.IsNullOrWhiteSpace(borrower.Phone) && !existingKeys.Contains(eventKey + ":sms"))
            Add(borrower, NotificationChannel.Sms, borrower.Phone, type, subject, message, eventKey + ":sms");
    }
}

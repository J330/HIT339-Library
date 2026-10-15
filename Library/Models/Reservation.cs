namespace S379658_HIT339_Assignment2_Library.Models;
public enum ReservationStatus { Pending, Ready, Collected, Cancelled, Expired }
public class Reservation
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime PlacedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadyAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? CollectedAtUtc { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace S379658_HIT339_Assignment2_Library.Models;

// A cash/payment receipt is a separate transaction; charging a fine is not payment.
public sealed class FinePayment
{
    public int Id { get; set; }
    public int BorrowingId { get; set; }
    public Borrowing Borrowing { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    [Required, StringLength(100)] public string Reference { get; set; } = "";
}

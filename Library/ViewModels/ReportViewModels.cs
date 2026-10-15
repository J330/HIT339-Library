using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.ViewModels;

public enum ManagerReportType { Borrowing, Fines, Inventory, PaymentAudit }

public sealed class BranchSummaryRow
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = "";
    public int Copies { get; set; }
    public int Available { get; set; }
    public int DamagedOrDestroyed { get; set; }
    public int ActiveLoans { get; set; }
    public int PendingReservations { get; set; }
    public int ReadyReservations { get; set; }
    public int PendingTransfers { get; set; }
}

public sealed class ExtendedManagerDashboard
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<BranchSummaryRow> Branches { get; set; } = new();
    public int TotalPendingReservations => Branches.Sum(r => r.PendingReservations);
    public int TotalReadyReservations => Branches.Sum(r => r.ReadyReservations);
    public int TotalPendingTransfers => Branches.Sum(r => r.PendingTransfers);
    public decimal TotalFinesCharged { get; set; }
    public decimal TotalPaymentsReceived { get; set; }
    public decimal OutstandingBalance => TotalFinesCharged - TotalPaymentsReceived;
}

public sealed class ReportTable
{
    public string Title { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string[] Columns { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; set; } = new();
}

public sealed class ReportPageViewModel
{
    public ManagerReportType Type { get; set; } = ManagerReportType.Borrowing;
    public int? BranchId { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
    public ReportTable Table { get; set; } = new();
}

public sealed class FinePaymentInput
{
    [Range(1, int.MaxValue)] public int BorrowingId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Enter a positive payment amount.")]
    public decimal Amount { get; set; }
    [Required, StringLength(100)] public string Reference { get; set; } = "";
}

public sealed class FinePaymentRow
{
    public int BorrowingId { get; set; }
    public string Borrower { get; set; } = "";
    public string Item { get; set; } = "";
    public decimal Charged { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding => Charged - Paid;
}

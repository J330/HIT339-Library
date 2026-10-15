namespace S379658_HIT339_Assignment2_Library.ViewModels;

public class ManagerDashboardViewModel
{
    public BorrowingStatisticsViewModel Borrowing { get; set; } = new();
    public ItemStatisticsViewModel Items { get; set; } = new();
    public FineStatisticsViewModel Fines { get; set; } = new();
    public DateTime GeneratedOn { get; set; }
}

public class BorrowingStatisticsViewModel
{
    public int TotalBorrowings { get; set; }
    public int ActiveBorrowings { get; set; }
    public int ReturnedBorrowings { get; set; }
    public int OverdueBorrowings { get; set; }
    public int OnTimeReturns { get; set; }
    public int LateReturns { get; set; }
}

public class ItemStatisticsViewModel
{
    public int TotalItems { get; set; }
    public int AvailableItems { get; set; }
    public int BorrowedItems { get; set; }
    public int DamagedItems { get; set; }
    public int DestroyedItems { get; set; }
    public int Books { get; set; }
    public int Music { get; set; }
    public int Toys { get; set; }
}

public class FineStatisticsViewModel
{
    public decimal TotalFinesCharged { get; set; }
    public decimal TotalPaymentsReceived { get; set; }
    public decimal OutstandingFineBalance => TotalFinesCharged - TotalPaymentsReceived;
    public int BorrowingsWithFines { get; set; }
    public decimal AverageFine { get; set; }
    public decimal HighestFine { get; set; }
    public decimal EstimatedCurrentOverdueFines { get; set; }
    public int CurrentOverdueItems { get; set; }
}

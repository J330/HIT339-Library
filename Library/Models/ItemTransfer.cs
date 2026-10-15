namespace S379658_HIT339_Assignment2_Library.Models;
public enum TransferStatus { Pending, Completed, Cancelled }
public class ItemTransfer
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int SourceBranchId { get; set; }
    public LibraryBranch SourceBranch { get; set; } = null!;
    public int DestinationBranchId { get; set; }
    public LibraryBranch DestinationBranch { get; set; } = null!;
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string Notes { get; set; } = "";
}

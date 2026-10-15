namespace S379658_HIT339_Assignment2_Library.Models;

public abstract class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LibraryCode { get; set; } = string.Empty;
    public ItemStatus Status { get; set; } = ItemStatus.Available;
    // A row describes one physical copy, so each copy has exactly one holding branch.
    public int BranchId { get; set; } = 1;
    public LibraryBranch Branch { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<ItemTransfer> Transfers { get; set; } = new List<ItemTransfer>();

    // Inverse navigation for the one-to-many Item -> Borrowings relationship.
    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}

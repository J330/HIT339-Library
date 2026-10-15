using S379658_HIT339_Assignment2_Library.Models;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public class CatalogueSearchViewModel
{
    public string? SearchString { get; set; }
    public string? ItemType { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public int? BranchId { get; set; }
    public List<LibraryBranch> Branches { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public HashSet<int> HeldItemIds { get; set; } = new();
    // Borrower-specific data; never returned to anonymous users or the public catalogue API.
    public List<BorrowedBookSummaryViewModel> MyBorrowedBooks { get; set; } = new();

    public int ResultCount => Items.Count;
}

// A concise loan status model used only in authenticated MVC views.
public sealed class BorrowedBookSummaryViewModel
{
    public int ItemId { get; set; }
    public string Title { get; set; } = "";
    public string BranchName { get; set; } = "";
    public DateTime BorrowedOn { get; set; }
    public DateTime DueOn { get; set; }
    public int DaysRemaining => (DueOn.Date - DateTime.Today).Days;
}

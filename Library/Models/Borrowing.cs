using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace S379658_HIT339_Assignment2_Library.Models;

public class Borrowing
{
    public int Id { get; set; }

    [Display(Name = "Checkout method")]
    public CheckoutChannel Channel { get; set; } = CheckoutChannel.Reception;

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int CheckoutBranchId { get; set; }
    public LibraryBranch CheckoutBranch { get; set; } = null!;
    public int? ReturnBranchId { get; set; }
    public LibraryBranch? ReturnBranch { get; set; }

    public int BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    [Display(Name = "Borrow Date")]
    [DataType(DataType.Date)]
    public DateTime BorrowDate { get; set; }

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; }

    [Display(Name = "Return Date")]
    [DataType(DataType.Date)]
    public DateTime? ReturnDate { get; set; }

    [Display(Name = "Fine Amount")]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal FineAmount { get; set; }
}

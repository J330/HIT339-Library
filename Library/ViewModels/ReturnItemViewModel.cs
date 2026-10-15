using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public class ReturnItemViewModel
{
    public int BorrowingId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Select the receiving branch.")]
    [Display(Name = "Return / Receiving Branch")]
    public int ReturnBranchId { get; set; }

    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; }

    [Required]
    [Display(Name = "Return Date")]
    [DataType(DataType.Date)]
    public DateTime ReturnDate { get; set; } = DateTime.Today;

    [Display(Name = "Estimated Fine")]
    public decimal EstimatedFine { get; set; }
}

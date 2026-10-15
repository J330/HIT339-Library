using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public class BorrowItemViewModel
{
    [Required]
    [Display(Name = "Library Code")]
    public string LibraryCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a borrower.")]
    [Display(Name = "Borrower")]
    public int BorrowerId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Select the issuing branch.")]
    [Display(Name = "Reception Branch")]
    public int BranchId { get; set; }

    [Required]
    [Display(Name = "Borrow Date")]
    [DataType(DataType.Date)]
    public DateTime BorrowDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
}

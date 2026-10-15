using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public sealed class OnlineBorrowViewModel
{
    [Range(1, int.MaxValue)]
    public int ItemId { get; set; }

    [Display(Name = "Borrow for (days)")]
    [Range(1, 14, ErrorMessage = "Choose between 1 and 14 days.")]
    public int LoanDays { get; set; } = 14;
}

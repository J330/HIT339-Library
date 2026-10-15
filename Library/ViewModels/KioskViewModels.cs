using System.ComponentModel.DataAnnotations;
using S379658_HIT339_Assignment2_Library.Models;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public sealed class KioskLoginViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Username, email or membership code")]
    public string MembershipCode { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public sealed class KioskCheckoutViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "Item barcode / library code")]
    public string LibraryCode { get; set; } = "";
}

public sealed class KioskChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class KioskAccountViewModel
{
    public string MemberName { get; set; } = "";
    public string MembershipCode { get; set; } = "";
    public string Username { get; set; } = "";
    public List<KioskLoanViewModel> ActiveLoans { get; set; } = new();
    public decimal RecordedFines { get; set; }
}

public sealed class KioskLoanViewModel
{
    public string ItemName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool Overdue { get; set; }
    public int DaysRemaining => (DueDate.Date - DateTime.Today).Days;
}

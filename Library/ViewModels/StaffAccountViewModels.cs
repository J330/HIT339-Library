using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public sealed class CreateStaffAccountViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, underscores or hyphens.")]
    public string Username { get; set; } = "";

    [Required, EmailAddress, StringLength(100)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "Staff role")]
    public string Role { get; set; } = "Reception";

    [Required, StringLength(100, MinimumLength = 12), DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string TemporaryPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(TemporaryPassword))]
    [Display(Name = "Confirm temporary password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class StaffAccountListViewModel
{
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Roles { get; set; } = "";
    public bool MustChangePassword { get; set; }
}

public sealed class StaffChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 12), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

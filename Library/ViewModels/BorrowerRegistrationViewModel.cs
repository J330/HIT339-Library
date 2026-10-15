using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.ViewModels;

public sealed class BorrowerRegistrationViewModel
{
    [Required, StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required, StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, underscores or hyphens.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = "";

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = "";

    [Required, StringLength(200)]
    [Display(Name = "Residential address")]
    public string Address { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

using System.ComponentModel.DataAnnotations;
namespace S379658_HIT339_Assignment2_Library.ViewModels;
public sealed class MemberUsernameViewModel
{
    [Required, StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Use letters, numbers, dots, underscores or hyphens.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = "";
}

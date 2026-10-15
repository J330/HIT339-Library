using System.ComponentModel.DataAnnotations;

namespace S379658_HIT339_Assignment2_Library.Models;

public class Borrower
{
    public int Id { get; set; }

    [StringLength(24)]
    [Display(Name = "Membership Code")]
    public string MembershipCode { get; set; } = string.Empty;

    // Nullable so members created before kiosk expansion can be activated at reception.
    public Guid? IdentityUserId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<LibraryNotification> Notifications { get; set; } = new List<LibraryNotification>();
}

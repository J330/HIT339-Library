using Microsoft.AspNetCore.Identity;

namespace S379658_HIT339_Assignment2_Library.Users.Domain;

// Extends ASP.NET Core Identity instead of maintaining a custom password/user table.
public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Example of application-specific user data, following Microsoft's Identity guidance.
    [PersonalData]
    public string? FullName { get; set; }

    // Kiosk members must replace reception-issued passwords on first login or after reset.
    public bool MustChangePassword { get; set; }
}

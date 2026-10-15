using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Users.Domain;

namespace S379658_HIT339_Assignment2_Library.Services;

// Reception provisions borrower accounts; kiosk visitors cannot create or claim someone else's account.
public sealed class BorrowerAccountService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public BorrowerAccountService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public static string NewMembershipCode() => "LIB" + RandomNumberGenerator.GetHexString(12);
    private static string NewTemporaryPassword() => "K!a9" + RandomNumberGenerator.GetHexString(16);

    public async Task<string> CreateBorrowerWithAccountAsync(Borrower borrower)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        borrower.MembershipCode = NewMembershipCode();
        _db.Borrowers.Add(borrower);
        await _db.SaveChangesAsync();

        var password = await CreateAccountAsync(borrower, NewTemporaryPassword(), mustChangePassword: true);
        await transaction.CommitAsync();
        return password;
    }

    // Public registration creates a borrower and an Identity user in the same transaction.
    // Unlike reception-issued accounts, the password is chosen by the borrower.
    public async Task<ApplicationUser> RegisterBorrowerAsync(Borrower borrower, string password, string username)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        borrower.MembershipCode = NewMembershipCode();
        _db.Borrowers.Add(borrower);
        await _db.SaveChangesAsync();

        var user = await CreateIdentityUserAsync(borrower, password, mustChangePassword: false, username: username);
        await transaction.CommitAsync();
        return user;
    }

    // For borrowers created before this extension was installed.
    public async Task<string?> ActivateExistingBorrowerAsync(int borrowerId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var borrower = await _db.Borrowers.FirstOrDefaultAsync(b => b.Id == borrowerId);
        if (borrower is null || borrower.IdentityUserId.HasValue) return null;
        if (string.IsNullOrWhiteSpace(borrower.MembershipCode))
            borrower.MembershipCode = NewMembershipCode();

        var password = await CreateAccountAsync(borrower, NewTemporaryPassword(), mustChangePassword: true);
        await transaction.CommitAsync();
        return password;
    }

    public async Task<string?> ResetPasswordAsync(int borrowerId)
    {
        var borrower = await _db.Borrowers.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == borrowerId);
        if (borrower?.IdentityUserId is not Guid id) return null;

        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return null;

        var password = NewTemporaryPassword();
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var result = await _users.ResetPasswordAsync(user, token, password);
        EnsureSucceeded(result);
        user.MustChangePassword = true;
        EnsureSucceeded(await _users.UpdateAsync(user));
        EnsureSucceeded(await _users.SetLockoutEndDateAsync(user, null));
        EnsureSucceeded(await _users.ResetAccessFailedCountAsync(user));
        return password;
    }

    private async Task<string> CreateAccountAsync(Borrower borrower, string password, bool mustChangePassword)
    {
        await CreateIdentityUserAsync(borrower, password, mustChangePassword);
        return password;
    }

    private async Task<ApplicationUser> CreateIdentityUserAsync(Borrower borrower, string password, bool mustChangePassword, string? username = null)
    {
        var fullName = $"{borrower.FirstName} {borrower.LastName}".Trim();
        if (fullName.Length > 100) fullName = fullName[..100];
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username ?? borrower.MembershipCode,
            Email = borrower.Email,
            FullName = fullName,
            LockoutEnabled = true,
            MustChangePassword = mustChangePassword
        };

        EnsureSucceeded(await _users.CreateAsync(user, password));
        EnsureSucceeded(await _users.AddToRoleAsync(user, AppRoles.Borrower));
        borrower.IdentityUserId = user.Id;
        await _db.SaveChangesAsync();
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

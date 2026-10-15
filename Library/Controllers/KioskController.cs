using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

// Separate large-control layout, no account data in URLs, POST + CSRF for state changes.
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class KioskController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly KioskCheckoutService _checkout;

    public KioskController(AppDbContext db, UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn, KioskCheckoutService checkout)
    {
        _db = db;
        _users = users;
        _signIn = signIn;
        _checkout = checkout;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Borrower)) return RedirectToAction(nameof(Account));
        return View();
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login() => View(new KioskLoginViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("kiosk-login")]
    public async Task<IActionResult> Login(KioskLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var identifier = model.MembershipCode.Trim();
        // A username may differ from the permanent barcode; match both without changing the schema.
        var borrowerUserId = await _db.Borrowers.AsNoTracking()
            .Where(b => b.MembershipCode == identifier.ToUpper())
            .Select(b => b.IdentityUserId).FirstOrDefaultAsync();
        ApplicationUser? user = borrowerUserId.HasValue
            ? await _users.FindByIdAsync(borrowerUserId.Value.ToString())
            : null;
        user ??= await _users.FindByNameAsync(identifier);
        if (user is null && identifier.Contains('@'))
            user = await _users.FindByEmailAsync(identifier);
        if (user is null || !await _users.IsInRoleAsync(user, AppRoles.Borrower) ||
            !await _db.Borrowers.AnyAsync(b => b.IdentityUserId == user.Id))
        {
            ModelState.AddModelError("", "Username, email, membership code or password was not recognised.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password,
            isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.IsLockedOut
                ? "Account temporarily locked. Please contact reception."
                : "Username, email, membership code or password was not recognised.");
            return View(model);
        }

        return RedirectToAction(user.MustChangePassword ? nameof(ChangePassword) : nameof(Account));
    }

    [Authorize(Roles = AppRoles.Borrower), HttpGet]
    public async Task<IActionResult> Account()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));

        var member = await _db.Borrowers.AsNoTracking()
            .Where(b => b.IdentityUserId == user.Id)
            .Select(b => new { b.Id, b.FirstName, b.LastName, b.MembershipCode })
            .SingleOrDefaultAsync();
        if (member is null) return Forbid();

        var loans = await _db.Borrowings.AsNoTracking()
            .Where(b => b.BorrowerId == member.Id && b.ReturnDate == null)
            .OrderBy(b => b.DueDate)
            .Select(b => new KioskLoanViewModel
            {
                ItemName = b.Item.Name, BranchName = b.Item.Branch.Name,
                BorrowDate = b.BorrowDate, DueDate = b.DueDate,
                Overdue = b.DueDate < DateTime.Today
            }).ToListAsync();
        var fines = await _db.Borrowings.AsNoTracking()
            .Where(b => b.BorrowerId == member.Id)
            .SumAsync(b => (decimal?)b.FineAmount) ?? 0m;

        return View(new KioskAccountViewModel
        {
            MemberName = member.FirstName + " " + member.LastName,
            MembershipCode = member.MembershipCode,
            Username = user.UserName ?? member.MembershipCode,
            ActiveLoans = loans,
            RecordedFines = fines
        });
    }

    [Authorize(Roles = AppRoles.Borrower), HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        return View(new KioskCheckoutViewModel());
    }

    [Authorize(Roles = AppRoles.Borrower), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(KioskCheckoutViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        if (!ModelState.IsValid) return View(model);

        var memberId = await _db.Borrowers.AsNoTracking()
            .Where(b => b.IdentityUserId == user.Id).Select(b => (int?)b.Id)
            .SingleOrDefaultAsync();
        if (memberId is null) return Forbid();

        var result = await _checkout.CheckoutAsync(memberId.Value, model.LibraryCode);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.LibraryCode), result.Message);
            return View(model);
        }

        TempData["KioskSuccess"] = result.Message;
        return RedirectToAction(nameof(Account));
    }

    [Authorize(Roles = AppRoles.Borrower), HttpGet]
    public IActionResult ChangePassword() => View(new KioskChangePasswordViewModel());

    [Authorize(Roles = AppRoles.Borrower), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(KioskChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        user.MustChangePassword = false;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            ModelState.AddModelError("", "Password updated but account update failed. Contact reception.");
            return View(model);
        }
        await _signIn.RefreshSignInAsync(user);
        return RedirectToAction(nameof(Account));
    }

    [Authorize(Roles = AppRoles.Borrower), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class MemberAccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly BorrowerAccountService _accounts;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public MemberAccountController(AppDbContext db, BorrowerAccountService accounts,
        UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _db = db;
        _accounts = accounts;
        _users = users;
        _signIn = signIn;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new BorrowerRegistrationViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("borrower-register")]
    public async Task<IActionResult> Register(BorrowerRegistrationViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        model.FirstName = model.FirstName?.Trim() ?? "";
        model.LastName = model.LastName?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        model.Username = model.Username?.Trim() ?? "";
        model.Phone = model.Phone?.Trim() ?? "";
        model.Address = model.Address?.Trim() ?? "";
        ModelState.Clear();
        if (!TryValidateModel(model)) return View(model);
        if (await _users.FindByNameAsync(model.Username) is not null)
        {
            ModelState.AddModelError(nameof(model.Username), "This username is already in use.");
            return View(model);
        }

        // An existing reception-created record cannot be claimed simply by knowing its email.
        // The account owner must contact reception to activate that original record.
        var email = model.Email.ToUpperInvariant();
        if (await _db.Borrowers.AnyAsync(b => b.Email.ToUpper() == email) ||
            await _users.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email),
                "This email is already associated with a library account. Contact reception for help.");
            return View(model);
        }

        var borrower = new Borrower
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address
        };

        try
        {
            var user = await _accounts.RegisterBorrowerAsync(borrower, model.Password, model.Username);
            await _signIn.SignInAsync(user, isPersistent: false);
            TempData["MemberRegistrationSuccess"] =
                "Your library account has been created. Keep your membership code somewhere safe.";
            return RedirectToAction("Account", "Kiosk");
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError("", "We could not create your account. Check the information and try again.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "That account could not be created. Contact reception if you already registered.");
        }
        return View(model);
    }

    [Authorize(Roles = AppRoles.Borrower), HttpGet]
    public async Task<IActionResult> Details()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction("ChangePassword", "Kiosk");
        var borrower = await _db.Borrowers.AsNoTracking()
            .SingleOrDefaultAsync(b => b.IdentityUserId == user.Id);
        if (borrower is null) return Forbid();
        ViewBag.MembershipCode = borrower.MembershipCode;
        ViewBag.Username = user.UserName;
        return View(new BorrowerViewModel
        {
            Id = borrower.Id,
            FirstName = borrower.FirstName,
            LastName = borrower.LastName,
            Email = borrower.Email,
            Phone = borrower.Phone,
            Address = borrower.Address
        });
    }

    [Authorize(Roles = AppRoles.Borrower), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(BorrowerViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction("ChangePassword", "Kiosk");
        var borrower = await _db.Borrowers.SingleOrDefaultAsync(b => b.IdentityUserId == user.Id);
        if (borrower is null) return Forbid();

        ViewBag.MembershipCode = borrower.MembershipCode;
        ViewBag.Username = user.UserName;
        model.FirstName = model.FirstName?.Trim() ?? "";
        model.LastName = model.LastName?.Trim() ?? "";
        model.Email = model.Email?.Trim() ?? "";
        model.Phone = model.Phone?.Trim() ?? "";
        model.Address = model.Address?.Trim() ?? "";
        ModelState.Clear();
        if (!TryValidateModel(model)) return View(model);

        var newEmail = model.Email.ToUpperInvariant();
        if (await _db.Borrowers.AnyAsync(b => b.Id != borrower.Id && b.Email.ToUpper() == newEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "This email is associated with another library account.");
            return View(model);
        }

        borrower.FirstName = model.FirstName;
        borrower.LastName = model.LastName;
        borrower.Email = model.Email;
        borrower.Phone = model.Phone;
        borrower.Address = model.Address;
        user.Email = model.Email;
        user.FullName = $"{model.FirstName} {model.LastName}";

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            foreach (var error in updated.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await _signIn.RefreshSignInAsync(user);
        TempData["ProfileSaved"] = "Contact details updated.";
        return RedirectToAction(nameof(Details));
    }

    [Authorize(Roles = AppRoles.Borrower), HttpGet]
    public async Task<IActionResult> Username()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction("ChangePassword", "Kiosk");
        if (!await _db.Borrowers.AnyAsync(b => b.IdentityUserId == user.Id)) return Forbid();
        return View(new MemberUsernameViewModel { Username = user.UserName ?? "" });
    }

    [Authorize(Roles = AppRoles.Borrower), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Username(MemberUsernameViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction("ChangePassword", "Kiosk");
        if (!await _db.Borrowers.AnyAsync(b => b.IdentityUserId == user.Id)) return Forbid();
        model.Username = model.Username?.Trim() ?? "";
        ModelState.Clear();
        if (!TryValidateModel(model)) return View(model);
        if (!await _users.CheckPasswordAsync(user, model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Your current password is incorrect.");
            return View(model);
        }
        if (!string.Equals(user.UserName, model.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (await _users.FindByNameAsync(model.Username) is not null)
            {
                ModelState.AddModelError(nameof(model.Username), "This username is already in use.");
                return View(model);
            }
            var update = await _users.SetUserNameAsync(user, model.Username);
            if (!update.Succeeded)
            {
                foreach (var error in update.Errors) ModelState.AddModelError("", error.Description);
                return View(model);
            }
            await _signIn.RefreshSignInAsync(user);
        }
        TempData["UsernameSaved"] = "Your username has been updated. Your membership barcode will still work.";
        return RedirectToAction(nameof(Username));
    }
}

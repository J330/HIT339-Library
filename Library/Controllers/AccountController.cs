using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        if (user.MustChangePassword && (await _userManager.IsInRoleAsync(user, AppRoles.Admin) ||
            await _userManager.IsInRoleAsync(user, AppRoles.Manager) ||
            await _userManager.IsInRoleAsync(user, AppRoles.Reception)))
            return RedirectToAction(nameof(ChangePassword));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(AppRoles.Borrower))
        {
            return RedirectToAction("Account", "Kiosk");
        }

        if (roles.Contains(AppRoles.Admin))
        {
            return RedirectToAction("Index", "AdminItems");
        }

        if (roles.Contains(AppRoles.Reception))
        {
            return RedirectToAction("Index", "Borrowing");
        }

        if (roles.Contains(AppRoles.Manager))
        {
            return RedirectToAction("Index", "Managers");
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Reception)]
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        ViewBag.FirstLogin = user.MustChangePassword;
        return View(new StaffChangePasswordViewModel());
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Reception)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(StaffChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        ViewBag.FirstLogin = user.MustChangePassword;
        if (!ModelState.IsValid) return View(model);
        if (model.NewPassword.Length < 12 || !model.NewPassword.Any(char.IsUpper) ||
            !model.NewPassword.Any(char.IsLower) || !model.NewPassword.Any(char.IsDigit) ||
            !model.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
        {
            ModelState.AddModelError(nameof(model.NewPassword),
                "Use at least 12 characters with uppercase, lowercase, a number and a symbol.");
            return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        user.MustChangePassword = false;
        var updated = await _userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            ModelState.AddModelError("", "Password updated, but the account update failed. Please contact an administrator.");
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(user);
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            return RedirectToAction("Index", "StaffAccounts");
        if (await _userManager.IsInRoleAsync(user, AppRoles.Manager))
            return RedirectToAction("Index", "Managers");
        return RedirectToAction("Index", "Borrowing");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

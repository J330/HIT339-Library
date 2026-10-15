using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

// A manager or receptionist must never be able to create or promote staff accounts.
[Authorize(Roles = AppRoles.Admin)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class StaffAccountsController : Controller
{
    private static readonly string[] AllowedRoles =
        { AppRoles.Admin, AppRoles.Manager, AppRoles.Reception };

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<StaffAccountsController> _logger;

    public StaffAccountsController(AppDbContext db, UserManager<ApplicationUser> users,
        ILogger<StaffAccountsController> logger)
    {
        _db = db;
        _users = users;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Query only staff records. Password hashes/security stamps are never sent to views.
        var records = await (
            from u in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles.AsNoTracking() on u.Id equals userRole.UserId
            join r in _db.Roles.AsNoTracking() on userRole.RoleId equals r.Id
            where r.Name == AppRoles.Admin || r.Name == AppRoles.Manager || r.Name == AppRoles.Reception
            orderby u.UserName
            select new { u.Id, u.FullName, u.UserName, u.Email, u.MustChangePassword, RoleName = r.Name }
        ).ToListAsync();

        var models = records.GroupBy(r => r.Id).Select(group =>
        {
            var user = group.First();
            return new StaffAccountListViewModel
            {
                UserId = user.Id,
                FullName = user.FullName ?? "",
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                Roles = string.Join(", ", group.Select(r => r.RoleName).Where(r => r != null).Distinct()),
                MustChangePassword = user.MustChangePassword
            };
        }).OrderBy(r => r.Username).ToList();

        return View(models);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateStaffAccountViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffAccountViewModel model)
    {
        model.FullName = (model.FullName ?? "").Trim();
        model.Username = (model.Username ?? "").Trim();
        model.Email = (model.Email ?? "").Trim();
        model.Role = (model.Role ?? "").Trim();

        ModelState.Clear();
        if (!TryValidateModel(model)) return View(model);

        if (!AllowedRoles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Role), "Choose Administrator, Manager or Receptionist.");

        // This policy applies to staff creation even though legacy development users
        // can have shorter passwords in the existing Identity configuration.
        var password = model.TemporaryPassword;
        if (!MeetsStaffPasswordPolicy(password))
            ModelState.AddModelError(nameof(model.TemporaryPassword),
                "Use 12 or more characters with uppercase, lowercase, a number and a symbol.");

        if (await _users.FindByNameAsync(model.Username) is not null)
            ModelState.AddModelError(nameof(model.Username), "This username is already in use.");
        if (await _users.FindByEmailAsync(model.Email) is not null)
            ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");

        if (!ModelState.IsValid) return View(model);

        // Both writes use the same Identity EF Core database context. A failed role
        // assignment must not leave an unprivileged, half-created account behind.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = false,
            LockoutEnabled = true,
            MustChangePassword = true
        };

        var created = await _users.CreateAsync(user, model.TemporaryPassword);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }

        var assigned = await _users.AddToRoleAsync(user, model.Role);
        if (!assigned.Succeeded)
        {
            foreach (var error in assigned.Errors) ModelState.AddModelError("", error.Description);
            await transaction.RollbackAsync();
            return View(model);
        }

        await transaction.CommitAsync();
        TempData["StaffAccountCreated"] = $"Staff account '{user.UserName}' created. " +
            "Give the temporary password to the staff member securely; they must change it on first sign-in.";
        return RedirectToAction(nameof(Index));
    }

    // The staff directory links to this page by ID. Both actions re-check that the
    // target is a staff account; an arbitrary borrower ID cannot be reset here.
    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var target = await FindEligibleTargetAsync(id);
        if (target is null) return NotFound();
        if (IsCurrentUser(target))
        {
            TempData["StaffPasswordError"] =
                "To change your own password, use the existing staff password-change page.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ResetStaffPasswordViewModel
        {
            UserId = target.Id,
            StaffUsername = target.UserName ?? "",
            StaffFullName = target.FullName ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetStaffPasswordViewModel model)
    {
        var target = await FindEligibleTargetAsync(model.UserId);
        if (target is null) return NotFound();
        if (IsCurrentUser(target)) return BadRequest("Use the staff password-change page for your own account.");

        // Names for display come only from the database, not the submitted form.
        model.StaffUsername = target.UserName ?? "";
        model.StaffFullName = target.FullName ?? "";

        if (!MeetsStaffPasswordPolicy(model.NewTemporaryPassword))
            ModelState.AddModelError(nameof(model.NewTemporaryPassword),
                "Use 12 or more characters with uppercase, lowercase, a number and a symbol.");
        if (!ModelState.IsValid) return View(model);

        // Identity resets the password hash and security stamp through its supported API.
        // The transaction also protects the required-first-login flag from partial writes.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var token = await _users.GeneratePasswordResetTokenAsync(target);
        var reset = await _users.ResetPasswordAsync(target, token, model.NewTemporaryPassword);
        if (!reset.Succeeded)
        {
            foreach (var error in reset.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }

        target.MustChangePassword = true;
        var saved = await _users.UpdateAsync(target);
        if (!saved.Succeeded)
        {
            foreach (var error in saved.Errors) ModelState.AddModelError("", error.Description);
            await transaction.RollbackAsync();
            return View(model);
        }

        // A password reset should also permit a staff member to sign in after a lockout.
        var unlocked = await _users.SetLockoutEndDateAsync(target, null);
        if (!unlocked.Succeeded)
        {
            foreach (var error in unlocked.Errors) ModelState.AddModelError("", error.Description);
            await transaction.RollbackAsync();
            return View(model);
        }
        var cleared = await _users.ResetAccessFailedCountAsync(target);
        if (!cleared.Succeeded)
        {
            foreach (var error in cleared.Errors) ModelState.AddModelError("", error.Description);
            await transaction.RollbackAsync();
            return View(model);
        }

        await transaction.CommitAsync();
        _logger.LogInformation("Administrator {AdminId} reset the staff password for {StaffId}",
            _users.GetUserId(User), target.Id);
        TempData["StaffPasswordReset"] =
            $"Temporary password set for '{target.UserName}'. Share it privately; " +
            "the staff member must change it when they next sign in.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> FindEligibleTargetAsync(Guid id)
    {
        if (id == Guid.Empty) return null;
        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return null;
        var roles = await _users.GetRolesAsync(user);
        return roles.Any(role => AllowedRoles.Contains(role, StringComparer.Ordinal)) ? user : null;
    }

    private bool IsCurrentUser(ApplicationUser target) =>
        string.Equals(_users.GetUserId(User), target.Id.ToString(), StringComparison.OrdinalIgnoreCase);

    private static bool MeetsStaffPasswordPolicy(string? password) =>
        !string.IsNullOrEmpty(password) && password.Length >= 12 &&
        password.Any(char.IsUpper) && password.Any(char.IsLower) &&
        password.Any(char.IsDigit) && password.Any(c => !char.IsLetterOrDigit(c));

}

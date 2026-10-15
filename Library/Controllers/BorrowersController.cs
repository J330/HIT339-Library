using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Reception + "," + AppRoles.Manager + "," + AppRoles.Admin)]
public class BorrowersController : Controller
{
    private readonly AppDbContext _context;
    private readonly BorrowerAccountService _accounts;

    public BorrowersController(AppDbContext context, BorrowerAccountService accounts)
    {
        _context = context;
        _accounts = accounts;
    }

    public async Task<IActionResult> Index(string? searchString)
    {
        var borrowers = _context.Borrowers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();
            borrowers = borrowers.Where(b =>
                b.FirstName.Contains(search) ||
                b.LastName.Contains(search) ||
                b.Email.Contains(search) ||
                b.Phone.Contains(search) ||
                b.MembershipCode.Contains(search));
        }

        ViewData["CurrentFilter"] = searchString;

        return View(await borrowers
            .OrderBy(b => b.LastName)
            .ThenBy(b => b.FirstName)
            .ToListAsync());
    }

    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .Include(b => b.Borrowings)
            .ThenInclude(b => b.Item)
            .FirstOrDefaultAsync(b => b.Id == id);

        return borrower == null ? NotFound() : View(borrower);
    }

    [Authorize(Roles = AppRoles.Reception)]
    public IActionResult Create()
    {
        return View(new BorrowerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> Create(BorrowerViewModel model)
    {
        Normalise(model);

        if (!ModelState.IsValid)
            return View(model);

        var borrower = new Borrower
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address
        };

        // Generates a unique membership barcode and provisions a hashed Identity password.
        var temporaryPassword = await _accounts.CreateBorrowerWithAccountAsync(borrower);
        TempData["NewKioskPassword"] = temporaryPassword; // Read and displayed only once, in reception UI.
        TempData["SuccessMessage"] = "Borrower created; provide the kiosk sign-in credentials privately.";
        return RedirectToAction(nameof(Details), new { id = borrower.Id });
    }

    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var borrower = await _context.Borrowers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (borrower == null)
            return NotFound();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> Edit(int id, BorrowerViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        Normalise(model);

        if (!ModelState.IsValid)
            return View(model);

        var borrower = await _context.Borrowers.FirstOrDefaultAsync(b => b.Id == id);
        if (borrower == null)
            return NotFound();

        borrower.FirstName = model.FirstName;
        borrower.LastName = model.LastName;
        borrower.Email = model.Email;
        borrower.Phone = model.Phone;
        borrower.Address = model.Address;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Borrowers.AnyAsync(b => b.Id == id))
                return NotFound();

            throw;
        }

        TempData["SuccessMessage"] = "Borrower updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var borrower = await _context.Borrowers
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        return borrower == null ? NotFound() : View(borrower);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var borrower = await _context.Borrowers
            .Include(b => b.Borrowings)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (borrower == null)
            return RedirectToAction(nameof(Index));

        if (borrower.IdentityUserId.HasValue)
        {
            TempData["ErrorMessage"] = "This borrower has a kiosk login. Account deletion must be handled separately.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (borrower.Borrowings.Any() ||
            await _context.Reservations.AnyAsync(r => r.BorrowerId == id) ||
            await _context.LibraryNotifications.AnyAsync(n => n.BorrowerId == id))
        {
            TempData["ErrorMessage"] =
                "This borrower cannot be deleted because they have borrowing, reservation or notification history. Edit their details instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Borrowers.Remove(borrower);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Borrower deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> ActivateKiosk(int id)
    {
        var password = await _accounts.ActivateExistingBorrowerAsync(id);
        if (password is null)
        {
            TempData["ErrorMessage"] = "Borrower not found or kiosk access already active.";
        }
        else
        {
            TempData["NewKioskPassword"] = password;
            TempData["SuccessMessage"] = "Kiosk access enabled. Give the temporary password directly to the borrower.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Reception)]
    public async Task<IActionResult> ResetKioskPassword(int id)
    {
        var password = await _accounts.ResetPasswordAsync(id);
        if (password is null)
        {
            TempData["ErrorMessage"] = "Borrower kiosk account not found.";
        }
        else
        {
            TempData["NewKioskPassword"] = password;
            TempData["SuccessMessage"] = "Password reset. The borrower must change it when they sign in.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private static void Normalise(BorrowerViewModel model)
    {
        model.FirstName = model.FirstName?.Trim() ?? string.Empty;
        model.LastName = model.LastName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim() ?? string.Empty;
        model.Phone = model.Phone?.Trim() ?? string.Empty;
        model.Address = model.Address?.Trim() ?? string.Empty;
    }
}

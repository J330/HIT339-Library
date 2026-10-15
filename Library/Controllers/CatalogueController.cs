using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

public class CatalogueController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly LibraryCheckoutService _checkout;

    public CatalogueController(AppDbContext context, UserManager<ApplicationUser> users,
        LibraryCheckoutService checkout)
    {
        _context = context;
        _users = users;
        _checkout = checkout;
    }

    // Public catalogue search. No staff account is required.
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? searchString,
        string? itemType,
        string? status,
        string? category,
        int? branchId)
    {
        IQueryable<Item> items = _context.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var search = searchString.Trim();

            items = items.Where(i =>
                i.Name.Contains(search) ||
                i.LibraryCode.Contains(search) ||
                i.Description.Contains(search) ||
                (i is Book && (((Book)i).Author.Contains(search) || ((Book)i).Genre.Contains(search))) ||
                (i is Music && (((Music)i).Artist.Contains(search) || ((Music)i).Genre.Contains(search))) ||
                (i is Toy && ((Toy)i).Type.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(itemType))
        {
            items = itemType switch
            {
                "Book" => items.Where(i => i is Book),
                "Music" => items.Where(i => i is Music),
                "Toy" => items.Where(i => i is Toy),
                _ => items
            };
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ItemStatus>(status, true, out var parsedStatus))
        {
            items = items.Where(i => i.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var term = category.Trim();
            items = items.Where(i =>
                (i is Book && ((Book)i).Genre.Contains(term)) ||
                (i is Music && ((Music)i).Genre.Contains(term)) ||
                (i is Toy && ((Toy)i).Type.Contains(term)));
        }

        if (branchId is > 0)
            items = items.Where(i => i.BranchId == branchId.Value);

        var model = new CatalogueSearchViewModel
        {
            SearchString = searchString,
            ItemType = itemType,
            Status = status,
            Category = category,
            BranchId = branchId,
            Branches = await _context.LibraryBranches.AsNoTracking()
                .Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(),
            Items = await items.Include(i => i.Branch)
                .OrderBy(i => i.Name)
                .ThenBy(i => i.LibraryCode)
                .ToListAsync()
        };

        var ids = model.Items.Select(i => i.Id).ToList();
        model.HeldItemIds = (await _context.Reservations.AsNoTracking()
            .Where(r => ids.Contains(r.ItemId) && r.Status == ReservationStatus.Ready)
            .Select(r => r.ItemId).ToListAsync()).ToHashSet();
        // Keep active loan details private to the authenticated borrower who owns them.
        if (User.IsInRole(AppRoles.Borrower))
        {
            var user = await _users.GetUserAsync(User);
            if (user != null && !user.MustChangePassword)
            {
                model.MyBorrowedBooks = await _context.Borrowings.AsNoTracking()
                    .Where(b => b.Borrower.IdentityUserId == user.Id &&
                                b.ReturnDate == null && b.Item is Book)
                    .OrderBy(b => b.DueDate)
                    .Select(b => new BorrowedBookSummaryViewModel
                    {
                        ItemId = b.ItemId,
                        Title = b.Item.Name,
                        BranchName = b.Item.Branch.Name,
                        BorrowedOn = b.BorrowDate,
                        DueOn = b.DueDate
                    }).ToListAsync();
            }
        }
        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.Items
            .AsNoTracking().Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        ViewBag.IsHeld = await _context.Reservations.AsNoTracking().AnyAsync(r =>
            r.ItemId == id && r.Status == ReservationStatus.Ready);
        return View(item);
    }

    [Authorize(Roles = AppRoles.Borrower)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BorrowOnline(OnlineBorrowViewModel model)
    {
        // Never trust the browser to choose arbitrary dates or an unlimited borrowing period.
        if (!ModelState.IsValid || model.LoanDays is < 1 or > 14 || model.ItemId <= 0)
        {
            TempData["OnlineBorrowError"] = "Choose a borrowing period between 1 and 14 days.";
            return RedirectToAction(nameof(Details), new { id = model.ItemId });
        }
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (user.MustChangePassword) return RedirectToAction("ChangePassword", "Kiosk");

        var borrowerId = await _context.Borrowers.AsNoTracking()
            .Where(b => b.IdentityUserId == user.Id)
            .Select(b => (int?)b.Id).SingleOrDefaultAsync();
        if (!borrowerId.HasValue) return Forbid();
        // The item and library code come from the database rather than the POST payload.
        var item = await _context.Books.AsNoTracking()
            .Where(b => b.Id == model.ItemId)
            .Select(b => new { b.Id, b.LibraryCode }).SingleOrDefaultAsync();
        if (item == null) return NotFound();

        var result = await _checkout.CheckoutAsync(borrowerId.Value, item.LibraryCode,
            DateTime.Today, DateTime.Today.AddDays(model.LoanDays), channel: CheckoutChannel.Online);
        if (!result.Succeeded)
        {
            TempData["OnlineBorrowError"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = model.ItemId });
        }
        TempData["KioskSuccess"] = result.Message + " This is a physical library copy; arrange collection with its holding branch.";
        return RedirectToAction("Account", "Kiosk");
    }

}

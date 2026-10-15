using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.ViewModels;
namespace S379658_HIT339_Assignment2_Library.Controllers;
[Authorize(Roles = AppRoles.Reception + "," + AppRoles.Manager + "," + AppRoles.Admin)]
public class BorrowingController : Controller
{
    private readonly AppDbContext _db;
    private readonly FineService _fine;
    private readonly LibraryCheckoutService _checkout;
    private readonly ReservationService _reservations;
    private readonly NotificationService _notifications;
    public BorrowingController(AppDbContext db, FineService fine, LibraryCheckoutService checkout,
        ReservationService reservations, NotificationService notifications)
    { _db = db; _fine = fine; _checkout = checkout; _reservations = reservations; _notifications = notifications; }

    public async Task<IActionResult> Index() => View(await _db.Borrowings.AsNoTracking()
        .Include(b => b.Item).Include(b => b.Borrower).Include(b => b.CheckoutBranch).Include(b => b.ReturnBranch)
        .OrderBy(b => b.ReturnDate != null).ThenBy(b => b.DueDate).ToListAsync());

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null) return NotFound();
        var borrowing = await _db.Borrowings.AsNoTracking().Include(b => b.Item)
            .Include(b => b.Borrower).Include(b => b.CheckoutBranch).Include(b => b.ReturnBranch)
            .FirstOrDefaultAsync(b => b.Id == id);
        return borrowing == null ? NotFound() : View(borrowing);
    }

    [HttpGet]
    public async Task<IActionResult> EditDueDate(int id)
    {
        var loan = await _db.Borrowings.AsNoTracking().Include(b => b.Item).Include(b => b.Borrower)
            .SingleOrDefaultAsync(b => b.Id == id);
        if (loan == null) return NotFound();
        if (loan.ReturnDate != null)
        {
            TempData["ErrorMessage"] = "A returned loan cannot have its due date edited.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new EditLoanDueDateViewModel
        {
            Id = loan.Id, DueDate = loan.DueDate, BorrowDate = loan.BorrowDate,
            ItemName = loan.Item.Name,
            BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDueDate(EditLoanDueDateViewModel model)
    {
        var loan = await _db.Borrowings.Include(b => b.Item).Include(b => b.Borrower)
            .SingleOrDefaultAsync(b => b.Id == model.Id);
        if (loan == null) return NotFound();
        if (loan.ReturnDate != null)
        {
            TempData["ErrorMessage"] = "This loan has already been returned.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        model.ItemName = loan.Item.Name;
        model.BorrowerName = loan.Borrower.FirstName + " " + loan.Borrower.LastName;
        model.BorrowDate = loan.BorrowDate;
        if (model.DueDate.Date <= loan.BorrowDate.Date || model.DueDate.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.DueDate), "Choose a future due date after the original borrowing date.");
        if (!ModelState.IsValid) return View(model);
        loan.DueDate = model.DueDate.Date;
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Due date updated. The new date will appear on the notification calendar.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Borrow()
    {
        await PopulateDropDowns();
        return View(new BorrowItemViewModel { BorrowDate = DateTime.Today, DueDate = DateTime.Today.AddDays(14) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowItemViewModel model)
    {
        model.LibraryCode = model.LibraryCode?.Trim() ?? "";
        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(model.BorrowerId, model.BranchId); return View(model);
        }
        var result = await _checkout.CheckoutAsync(model.BorrowerId, model.LibraryCode,
            model.BorrowDate, model.DueDate, model.BranchId);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.Message);
            await PopulateDropDowns(model.BorrowerId, model.BranchId); return View(model);
        }
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Return(string? libraryCode)
    {
        var model = new ReturnItemViewModel { LibraryCode = libraryCode?.Trim() ?? "", ReturnDate = DateTime.Today };
        await PopulateBranches();
        if (string.IsNullOrWhiteSpace(model.LibraryCode)) return View(model);
        var borrowing = await _db.Borrowings.AsNoTracking().Include(b => b.Item).Include(b => b.Borrower)
            .FirstOrDefaultAsync(b => b.Item.LibraryCode == model.LibraryCode && b.ReturnDate == null);
        if (borrowing == null)
        { ViewData["ReturnError"] = "No active borrowing was found for that library code."; return View(model); }
        PopulateReturn(model, borrowing);
        model.ReturnBranchId = borrowing.CheckoutBranchId;
        await PopulateBranches(model.ReturnBranchId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(ReturnItemViewModel model)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var borrowing = await _db.Borrowings.Include(b => b.Item).Include(b => b.Borrower)
            .FirstOrDefaultAsync(b => b.Id == model.BorrowingId && b.ReturnDate == null);
        if (borrowing == null) { ModelState.AddModelError("", "Active loan not found."); await PopulateBranches(); return View(model); }
        if (model.ReturnDate.Date < borrowing.BorrowDate.Date)
            ModelState.AddModelError(nameof(model.ReturnDate), "Return date cannot be before the borrow date.");
        if (!await _db.LibraryBranches.AnyAsync(b => b.Id == model.ReturnBranchId && b.IsActive))
            ModelState.AddModelError(nameof(model.ReturnBranchId), "Select an active receiving branch.");
        if (!ModelState.IsValid)
        { PopulateReturn(model, borrowing); await PopulateBranches(model.ReturnBranchId); return View(model); }
        borrowing.ReturnDate = model.ReturnDate.Date;
        borrowing.ReturnBranchId = model.ReturnBranchId;
        borrowing.FineAmount = _fine.CalculateFine(borrowing.DueDate, model.ReturnDate);
        borrowing.Item.BranchId = model.ReturnBranchId;
        borrowing.Item.Status = ItemStatus.Available;
        if (borrowing.FineAmount > 0)
            _notifications.Queue(borrowing.Borrower, NotificationType.Fine, "Library return fine",
                $"{borrowing.Item.Name} was returned late. Fine charged: {borrowing.FineAmount:C}.",
                $"return-fine:{borrowing.Id}");
        await _db.SaveChangesAsync();
        await _reservations.PromoteNextAsync(borrowing.Item);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["SuccessMessage"] = borrowing.FineAmount > 0
            ? $"{borrowing.Item.Name} returned. Fine: {borrowing.FineAmount:C}." : $"{borrowing.Item.Name} returned.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDowns(int? borrower = null, int? branch = null)
    {
        var people = await _db.Borrowers.AsNoTracking().OrderBy(b => b.LastName).ThenBy(b => b.FirstName)
            .Select(b => new { b.Id, Name = b.FirstName + " " + b.LastName + " - " + b.Email }).ToListAsync();
        ViewBag.BorrowerId = new SelectList(people, "Id", "Name", borrower);
        await PopulateBranches(branch);
    }
    private async Task PopulateBranches(int? selected = null)
    {
        var branches = await _db.LibraryBranches.AsNoTracking().Where(b => b.IsActive)
            .OrderBy(b => b.Name).ToListAsync();
        ViewBag.BranchId = new SelectList(branches, "Id", "Name", selected);
    }
    private void PopulateReturn(ReturnItemViewModel model, Borrowing borrowing)
    {
        model.BorrowingId = borrowing.Id; model.LibraryCode = borrowing.Item.LibraryCode;
        model.ItemName = borrowing.Item.Name;
        model.BorrowerName = borrowing.Borrower.FirstName + " " + borrowing.Borrower.LastName;
        model.DueDate = borrowing.DueDate;
        model.EstimatedFine = _fine.CalculateFine(borrowing.DueDate, model.ReturnDate);
    }
}

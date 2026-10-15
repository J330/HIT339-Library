using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using S379658_HIT339_Assignment2_Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminItemsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ReservationService _reservations;

    public AdminItemsController(AppDbContext context, ReservationService reservations)
    {
        _context = context;
        _reservations = reservations;
    }
    private async Task PopulateBranches(int? selected = null)
    {
        var branches = await _context.LibraryBranches.AsNoTracking().Where(b => b.IsActive)
            .OrderBy(b => b.Name).ToListAsync();
        ViewBag.BranchId = new SelectList(branches, "Id", "Name", selected);
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.Items
            .AsNoTracking().Include(i => i.Branch)
            .OrderBy(i => i.Name)
            .ThenBy(i => i.LibraryCode)
            .ToListAsync();

        return View(items);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var item = await _context.Items
            .AsNoTracking().Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.Id == id);

        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateBranches();
        return View(new AdminItemViewModel
        {
            ItemType = LibraryItemType.Book,
            Status = ItemStatus.Available
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminItemViewModel model)
    {
        NormaliseCommon(model);

        if (model.Status == ItemStatus.Borrowed)
        {
            ModelState.AddModelError(nameof(model.Status),
                "A new item cannot start as Borrowed. Use the reception borrowing process after creating it.");
        }

        if (await _context.Items.AnyAsync(i => i.LibraryCode == model.LibraryCode))
        {
            ModelState.AddModelError(nameof(model.LibraryCode), "That library code is already in use.");
        }

        if (!await _context.LibraryBranches.AnyAsync(b => b.Id == model.BranchId && b.IsActive))
            ModelState.AddModelError(nameof(model.BranchId), "Select an active branch.");
        if (!ModelState.IsValid)
        {
            await PopulateBranches(model.BranchId);
            return View(model);
        }

        Item item = model.ItemType switch
        {
            LibraryItemType.Book => new Book
            {
                Author = model.Author!.Trim(),
                Genre = model.Genre!.Trim()
            },
            LibraryItemType.Music => new Music
            {
                Artist = model.Artist!.Trim(),
                Year = model.Year!.Value,
                Genre = model.Genre!.Trim()
            },
            LibraryItemType.Toy => new Toy
            {
                Type = model.ToyType!.Trim(),
                MinimumAge = model.MinimumAge!.Value
            },
            _ => throw new InvalidOperationException("Unsupported item type.")
        };

        ApplyCommonFields(item, model);
        item.BranchId = model.BranchId;

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{item.Name} was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id.Value);
        if (item == null)
            return NotFound();

        ViewBag.HasActiveBorrowing = await _context.Borrowings
            .AsNoTracking()
            .AnyAsync(b => b.ItemId == item.Id && b.ReturnDate == null);

        await PopulateBranches(item.BranchId);
        return View(ToViewModel(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminItemViewModel model)
    {
        if (id != model.Id)
            return NotFound();

        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return NotFound();

        var actualItemType = GetItemType(item);
        if (model.ItemType != actualItemType)
        {
            ModelState.AddModelError(nameof(model.ItemType), "The item type cannot be changed after creation.");
            model.ItemType = actualItemType;
        }

        NormaliseCommon(model);
        if (model.BranchId != item.BranchId)
            ModelState.AddModelError(nameof(model.BranchId), "Change the holding branch through Item Transfers.");
        if (await _context.ItemTransfers.AnyAsync(t => t.ItemId == id && t.Status == TransferStatus.Pending))
            ModelState.AddModelError("", "Complete or cancel the pending transfer before editing this item.");
        if (await _context.Reservations.AnyAsync(r => r.ItemId == id && r.Status == ReservationStatus.Ready) &&
            model.Status != ItemStatus.Available)
            ModelState.AddModelError(nameof(model.Status), "A copy held for pickup must remain available.");
        if (model.Status == ItemStatus.Destroyed && await _context.Reservations.AnyAsync(r => r.ItemId == id &&
            (r.Status == ReservationStatus.Ready || r.Status == ReservationStatus.Pending)))
            ModelState.AddModelError(nameof(model.Status), "Cancel existing reservations before destroying a copy.");

        if (await _context.Items.AnyAsync(i => i.Id != id && i.LibraryCode == model.LibraryCode))
        {
            ModelState.AddModelError(nameof(model.LibraryCode), "That library code is already in use.");
        }

        var hasActiveBorrowing = await _context.Borrowings
            .AnyAsync(b => b.ItemId == id && b.ReturnDate == null);

        ViewBag.HasActiveBorrowing = hasActiveBorrowing;

        if (hasActiveBorrowing && model.Status != ItemStatus.Borrowed)
        {
            ModelState.AddModelError(nameof(model.Status),
                "This item has an active borrowing and must remain Borrowed until it is returned at reception.");
        }
        else if (!hasActiveBorrowing && model.Status == ItemStatus.Borrowed)
        {
            ModelState.AddModelError(nameof(model.Status),
                "Borrowed status is controlled by the reception borrowing process and cannot be set manually.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateBranches(model.BranchId);
            return View(model);
        }

        var previousStatus = item.Status;
        ApplyCommonFields(item, model);

        switch (item)
        {
            case Book book:
                book.Author = model.Author!.Trim();
                book.Genre = model.Genre!.Trim();
                break;
            case Music music:
                music.Artist = model.Artist!.Trim();
                music.Year = model.Year!.Value;
                music.Genre = model.Genre!.Trim();
                break;
            case Toy toy:
                toy.Type = model.ToyType!.Trim();
                toy.MinimumAge = model.MinimumAge!.Value;
                break;
        }

        try
        {
            await _context.SaveChangesAsync();
            if (previousStatus != ItemStatus.Available && item.Status == ItemStatus.Available)
            {
                await _reservations.PromoteNextAsync(item);
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Items.AnyAsync(i => i.Id == id))
                return NotFound();

            throw;
        }

        TempData["SuccessMessage"] = $"{item.Name} was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return NotFound();

        ViewBag.HasBorrowingHistory = await _context.Borrowings.AnyAsync(b => b.ItemId == item.Id);
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return RedirectToAction(nameof(Index));

        if (await _context.Borrowings.AnyAsync(b => b.ItemId == id) ||
            await _context.Reservations.AnyAsync(r => r.ItemId == id) ||
            await _context.ItemTransfers.AnyAsync(t => t.ItemId == id))
        {
            TempData["ErrorMessage"] =
                "This item cannot be deleted because it has borrowing, reservation or transfer history. Edit its details instead.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{item.Name} was deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormaliseCommon(AdminItemViewModel model)
    {
        model.LibraryCode = model.LibraryCode?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim() ?? string.Empty;
    }

    private static void ApplyCommonFields(Item item, AdminItemViewModel model)
    {
        item.Name = model.Name;
        item.Description = model.Description;
        item.LibraryCode = model.LibraryCode;
        item.Status = model.Status;
    }

    private static AdminItemViewModel ToViewModel(Item item)
    {
        var model = new AdminItemViewModel
        {
            Id = item.Id,
            ItemType = GetItemType(item),
            Name = item.Name,
            Description = item.Description,
            LibraryCode = item.LibraryCode,
            Status = item.Status,
            BranchId = item.BranchId
        };

        switch (item)
        {
            case Book book:
                model.Author = book.Author;
                model.Genre = book.Genre;
                break;
            case Music music:
                model.Artist = music.Artist;
                model.Year = music.Year;
                model.Genre = music.Genre;
                break;
            case Toy toy:
                model.ToyType = toy.Type;
                model.MinimumAge = toy.MinimumAge;
                break;
        }

        return model;
    }

    private static LibraryItemType GetItemType(Item item) => item switch
    {
        Book => LibraryItemType.Book,
        Music => LibraryItemType.Music,
        Toy => LibraryItemType.Toy,
        _ => throw new InvalidOperationException("Unsupported item type.")
    };
}

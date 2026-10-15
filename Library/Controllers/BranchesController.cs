using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
namespace S379658_HIT339_Assignment2_Library.Controllers;
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
public sealed class BranchesController : Controller
{
    private readonly AppDbContext _db;
    public BranchesController(AppDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.LibraryBranches.AsNoTracking()
        .Include(b => b.ReceptionDesks).Include(b => b.Items).OrderBy(b => b.Name).ToListAsync());
    [HttpGet] public IActionResult Create() => View("Form", new LibraryBranch());
    [HttpGet] public async Task<IActionResult> Edit(int id)
    {
        var branch = await _db.LibraryBranches.FindAsync(id);
        return branch == null ? NotFound() : View("Form", branch);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(LibraryBranch input)
    {
        input.Code = (input.Code ?? "").Trim().ToUpperInvariant();
        input.Name = (input.Name ?? "").Trim();
        if (await _db.LibraryBranches.AnyAsync(b => b.Code == input.Code && b.Id != input.Id))
            ModelState.AddModelError(nameof(input.Code), "That branch code is taken.");
        if (input.Id > 0 && !input.IsActive &&
            await _db.Items.AnyAsync(i => i.BranchId == input.Id))
            ModelState.AddModelError(nameof(input.IsActive), "Move all physical copies to other branches before deactivating this branch.");
        if (!ModelState.IsValid) return View("Form", input);
        if (input.Id == 0) _db.LibraryBranches.Add(new LibraryBranch { Name = input.Name,
            Code = input.Code, Address = input.Address, Phone = input.Phone,
            OpeningHours = input.OpeningHours, IsActive = input.IsActive });
        else
        {
            var branch = await _db.LibraryBranches.FindAsync(input.Id);
            if (branch == null) return NotFound();
            branch.Name = input.Name; branch.Code = input.Code; branch.Address = input.Address;
            branch.Phone = input.Phone; branch.OpeningHours = input.OpeningHours;
            branch.IsActive = input.IsActive;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDesk(int branchId, string name)
    {
        if (!await _db.LibraryBranches.AnyAsync(b => b.Id == branchId)) return NotFound();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
        { TempData["ErrorMessage"] = "Desk name must be 1 to 80 characters."; return RedirectToAction(nameof(Index)); }
        _db.ReceptionDesks.Add(new ReceptionDesk { BranchId = branchId, Name = name.Trim() });
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

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
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Reception)]
public sealed class TransfersController : Controller
{
    private readonly AppDbContext _db;
    private readonly TransferService _transfers;
    public TransfersController(AppDbContext db, TransferService transfers)
    { _db = db; _transfers = transfers; }
    public async Task<IActionResult> Index() => View(await _db.ItemTransfers.AsNoTracking()
        .Include(t => t.Item).Include(t => t.SourceBranch).Include(t => t.DestinationBranch)
        .OrderBy(t => t.Status).ThenByDescending(t => t.RequestedAtUtc).ToListAsync());
    [HttpGet]
    public async Task<IActionResult> Create(int? itemId)
    {
        await DropDowns(itemId);
        return View(new TransferRequestViewModel { ItemId = itemId ?? 0 });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferRequestViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _transfers.RequestAsync(model.ItemId, model.DestinationBranchId, model.Notes);
            if (result.Ok) { TempData["SuccessMessage"] = result.Message; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError("", result.Message);
        }
        await DropDowns(model.ItemId); return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _transfers.CompleteAsync(id);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _transfers.CancelAsync(id);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
    private async Task DropDowns(int? selected = null)
    {
        var items = await _db.Items.AsNoTracking().OrderBy(i => i.Name)
            .Select(i => new { i.Id, Label = i.Name + " (" + i.LibraryCode + ")" }).ToListAsync();
        var branches = await _db.LibraryBranches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        ViewBag.ItemId = new SelectList(items, "Id", "Label", selected);
        ViewBag.DestinationBranchId = new SelectList(branches, "Id", "Name");
    }
}

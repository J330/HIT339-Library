using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.Users.Domain;
namespace S379658_HIT339_Assignment2_Library.Controllers;
[Authorize(Roles = AppRoles.Borrower + "," + AppRoles.Reception + "," + AppRoles.Admin)]
public sealed class ReservationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ReservationService _service;
    public ReservationsController(AppDbContext db, UserManager<ApplicationUser> users, ReservationService service)
    { _db = db; _users = users; _service = service; }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var query = _db.Reservations.AsNoTracking().Include(r => r.Item)
            .Include(r => r.Borrower).Include(r => r.Item.Branch).OrderBy(r => r.Item.Name).ThenBy(r => r.PlacedAtUtc).AsQueryable();
        if (User.IsInRole(AppRoles.Borrower))
        {
            var id = await CurrentBorrowerId();
            if (id == null) return Forbid();
            query = query.Where(r => r.BorrowerId == id.Value);
        }
        return View(await query.ToListAsync());
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = AppRoles.Borrower)]
    public async Task<IActionResult> Place(int itemId)
    {
        var id = await CurrentBorrowerId();
        if (id == null) return Forbid();
        var result = await _service.PlaceAsync(itemId, id.Value);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var isStaff = User.IsInRole(AppRoles.Reception) || User.IsInRole(AppRoles.Admin);
        var borrowerId = isStaff ? null : await CurrentBorrowerId();
        if (!isStaff && borrowerId == null) return Forbid();
        var result = await _service.CancelAsync(id, borrowerId, isStaff);
        TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
    private async Task<int?> CurrentBorrowerId()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null || user.MustChangePassword) return null;
        return await _db.Borrowers.AsNoTracking().Where(b => b.IdentityUserId == user.Id)
            .Select(b => (int?)b.Id).FirstOrDefaultAsync();
    }
}

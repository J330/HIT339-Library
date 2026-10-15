using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Models;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Reception + "," + AppRoles.Manager)]
public sealed class PaymentsController : Controller
{
    private readonly AppDbContext _db;
    public PaymentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await _db.Borrowings.AsNoTracking()
            .Where(b => b.FineAmount > 0)
            .Select(b => new FinePaymentRow
            {
                BorrowingId = b.Id,
                Borrower = b.Borrower.FirstName + " " + b.Borrower.LastName,
                Item = b.Item.Name,
                Charged = b.FineAmount,
                Paid = _db.FinePayments.Where(p => p.BorrowingId == b.Id).Sum(p => (decimal?)p.Amount) ?? 0
            }).OrderByDescending(b => b.BorrowingId).ToListAsync();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(FinePaymentInput input)
    {
        if (!ModelState.IsValid)
        { TempData["ErrorMessage"] = "Enter a borrowing ID, positive amount and reference (max 100 characters)."; return RedirectToAction(nameof(Index)); }
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var borrowing = await _db.Borrowings.FirstOrDefaultAsync(b => b.Id == input.BorrowingId && b.FineAmount > 0);
        if (borrowing is null) return NotFound();
        var paid = await _db.FinePayments.Where(p => p.BorrowingId == input.BorrowingId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        if (input.Amount > borrowing.FineAmount - paid)
        {
            TempData["ErrorMessage"] = "Amount exceeds the outstanding fine. No payment was recorded.";
            return RedirectToAction(nameof(Index));
        }
        _db.FinePayments.Add(new FinePayment
        { BorrowingId = borrowing.Id, Amount = input.Amount, Reference = input.Reference.Trim(), PaidAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["SuccessMessage"] = "Fine payment recorded successfully.";
        return RedirectToAction(nameof(Index));
    }
}

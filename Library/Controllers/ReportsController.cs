using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Manager)]
public sealed class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ManagerReportService _reports;
    private readonly ReportExportService _exports;
    public ReportsController(AppDbContext db, ManagerReportService reports, ReportExportService exports)
    { _db = db; _reports = reports; _exports = exports; }

    private bool Valid(ManagerReportType type, int? branchId, DateTime? from, DateTime? to) =>
        Enum.IsDefined(type) && (!branchId.HasValue || branchId.Value > 0) &&
        (!from.HasValue || !to.HasValue || from.Value.Date <= to.Value.Date);

    [HttpGet]
    public async Task<IActionResult> Index(ManagerReportType type = ManagerReportType.Borrowing,
        int? branchId = null, DateTime? from = null, DateTime? to = null)
    {
        if (!Valid(type, branchId, from, to)) return BadRequest("Invalid report filter or date range.");
        ViewBag.Branches = new SelectList(await _db.LibraryBranches.AsNoTracking().OrderBy(b => b.Name).ToListAsync(), "Id", "Name", branchId);
        return View(new ReportPageViewModel
        {
            Type = type, BranchId = branchId, From = from, To = to,
            Table = await _reports.BuildReportAsync(type, branchId, from, to)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(ManagerReportType type = ManagerReportType.Borrowing,
        string format = "csv", int? branchId = null, DateTime? from = null, DateTime? to = null)
    {
        if (!Valid(type, branchId, from, to)) return BadRequest("Invalid report filter or date range.");
        if (format is not ("csv" or "pdf")) return BadRequest("Select csv or pdf.");
        var table = await _reports.BuildReportAsync(type, branchId, from, to);
        var name = $"library-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}";
        return format == "csv"
            ? File(_exports.ToCsv(table), "text/csv; charset=utf-8", name + ".csv")
            : File(_exports.ToPdf(table), "application/pdf", name + ".pdf");
    }
}

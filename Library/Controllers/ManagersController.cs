using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Manager)]
public class ManagersController : Controller
{
    private readonly ManagerStatisticsService _statisticsService;
    private readonly ManagerReportService _reports;

    public ManagersController(ManagerStatisticsService statisticsService, ManagerReportService reports)
    {
        _statisticsService = statisticsService;
        _reports = reports;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;

        var model = new ManagerDashboardViewModel
        {
            Borrowing = await _statisticsService.BuildBorrowingStatisticsAsync(today),
            Items = await _statisticsService.BuildItemStatisticsAsync(),
            Fines = await _statisticsService.BuildFineStatisticsAsync(today),
            GeneratedOn = DateTime.Now
        };

        ViewBag.Extended = await _reports.OverviewAsync();
        return View(model);
    }

    public async Task<IActionResult> BorrowingStatistics()
    {
        return View(await _statisticsService.BuildBorrowingStatisticsAsync(DateTime.Today));
    }

    public async Task<IActionResult> ItemStatistics()
    {
        return View(await _statisticsService.BuildItemStatisticsAsync());
    }

    public async Task<IActionResult> FineStatistics()
    {
        return View(await _statisticsService.BuildFineStatisticsAsync(DateTime.Today));
    }
}

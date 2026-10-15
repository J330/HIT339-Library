using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using S379658_HIT339_Assignment2_Library.Database;
using S379658_HIT339_Assignment2_Library.Roles;
using S379658_HIT339_Assignment2_Library.Services;
using S379658_HIT339_Assignment2_Library.ViewModels;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Reception)]
public sealed class ImportsController : Controller
{
    private readonly AppDbContext _db;
    private readonly MediaImportService _imports;
    private readonly ExternalMetadataService _metadata;
    public ImportsController(AppDbContext db, MediaImportService imports, ExternalMetadataService metadata)
    { _db = db; _imports = imports; _metadata = metadata; }

    private async Task BranchChoices(int? branchId = null) => ViewBag.Branches = new SelectList(
        await _db.LibraryBranches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(),
        "Id", "Name", branchId);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await BranchChoices();
        return View(new MediaImportForm());
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2_200_000)]
    public async Task<IActionResult> Csv(MediaImportForm form)
    {
        var result = await _imports.ImportCsvAsync(form.File);
        await BranchChoices(form.BranchId);
        ViewBag.ImportResult = result;
        return View("Index", form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Mock(MediaImportForm form)
    {
        var result = await _imports.ImportMockAsync(form.BranchId);
        await BranchChoices(form.BranchId);
        ViewBag.ImportResult = result;
        return View("Index", form);
    }

    [HttpGet]
    public IActionResult SampleCsv()
    {
        var bytes = System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "sample-import.csv"));
        return File(bytes, "text/csv", "library-import-template.csv");
    }

    // Staff enter titles/authors/ISBNs/album names; results are never automatically saved.
    [HttpGet]
    public async Task<IActionResult> Search(string provider = "OpenLibrary", string searchType = "keyword",
        string query = "", CancellationToken cancellationToken = default)
    {
        var model = new ExternalSearchViewModel { Provider = provider, SearchType = searchType, Query = query };
        if (string.IsNullOrWhiteSpace(query)) return View(model);
        model.Searched = true;
        try { model.Results = await _metadata.SearchAsync(provider, searchType, query, cancellationToken); }
        catch (ArgumentException ex) { model.Error = ex.Message; }
        catch (HttpRequestException) { model.Error = "The external provider is unavailable. Try again or use the offline mock data."; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { model.Error = "The metadata provider timed out. Please try again."; }
        catch (JsonException) { model.Error = "The provider returned an unreadable response."; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(string provider, string externalId, CancellationToken cancellationToken)
    {
        ExternalMediaRecord? record;
        try { record = await _metadata.GetByIdAsync(provider, externalId, cancellationToken); }
        catch (HttpRequestException) { TempData["ImportError"] = "Metadata provider unavailable. Try again."; return RedirectToAction(nameof(Search)); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { TempData["ImportError"] = "The provider timed out."; return RedirectToAction(nameof(Search)); }
        catch (JsonException) { TempData["ImportError"] = "The provider returned invalid data."; return RedirectToAction(nameof(Search)); }
        if (record is null) return NotFound("That metadata record was not found.");
        var model = new ExternalImportForm
        {
            Provider = record.Provider, ExternalId = record.ExternalId,
            Creator = record.Creator, Genre = record.Genre,
            Year = record.Year, MinimumAge = record.MinimumAge,
            Description = record.Description
        };
        await SetupPreviewAsync(record, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(ExternalImportForm form, CancellationToken cancellationToken)
    {
        ExternalMediaRecord? record;
        try { record = await _metadata.GetByIdAsync(form.Provider, form.ExternalId, cancellationToken); }
        catch (HttpRequestException) { ModelState.AddModelError("", "Metadata provider unavailable. Import was not saved."); record = null; }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { ModelState.AddModelError("", "Metadata provider timed out. Import was not saved."); record = null; }
        catch (JsonException) { ModelState.AddModelError("", "Provider returned invalid metadata. Import was not saved."); record = null; }
        if (record is null)
        {
            ModelState.AddModelError("", "Could not verify the selected item; nothing was imported.");
            TempData["ImportError"] = "Could not verify the selected item. Nothing was imported.";
            return RedirectToAction(nameof(Search));
        }
        if (!ModelState.IsValid)
        {
            await SetupPreviewAsync(record, form);
            return View("Preview", form);
        }
        var outcome = await _imports.ImportExternalAsync(record, form);
        if (!outcome.Success)
        {
            foreach (var error in outcome.Errors) ModelState.AddModelError("", error);
            await SetupPreviewAsync(record, form);
            return View("Preview", form);
        }
        TempData["ImportSuccess"] = $"Imported {record.Title} as copy {form.LibraryCode.Trim()}.";
        return RedirectToAction(nameof(Search));
    }

    private async Task SetupPreviewAsync(ExternalMediaRecord record, ExternalImportForm form)
    {
        await BranchChoices(form.BranchId);
        ViewBag.ExternalRecord = record;
        ViewBag.ExistingCopies = await _db.Items.AsNoTracking()
            .CountAsync(i => i.Name == record.Title);
    }
}

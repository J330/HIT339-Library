using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using S379658_HIT339_Assignment2_Library.Roles;

namespace S379658_HIT339_Assignment2_Library.Controllers;

// The package lives OUTSIDE wwwroot. Authorize both the page AND the file response.
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Reception)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DownloadsController : Controller
{
    private const string PackageFileName = "LibraryDesktop-Windows-x64.zip";
    private readonly IWebHostEnvironment _hosting;
    private readonly IConfiguration _configuration;

    public DownloadsController(IWebHostEnvironment hosting, IConfiguration configuration)
    {
        _hosting = hosting;
        _configuration = configuration;
    }

    private string PackagePath()
    {
        // An administrator may configure an absolute private path on Azure.
        // Browser input never influences this filesystem path.
        var configured = _configuration["Downloads:WindowsPackagePath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(_hosting.ContentRootPath, "App_Data", "Downloads", PackageFileName)
            : Path.IsPathRooted(configured) ? configured : Path.Combine(_hosting.ContentRootPath, configured);
        var fullPath = Path.GetFullPath(path);
        if (!string.IsNullOrWhiteSpace(_hosting.WebRootPath))
        {
            var publicRoot = Path.GetFullPath(_hosting.WebRootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(publicRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The staff download must be stored outside wwwroot.");
        }
        return fullPath;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.PackageReady = System.IO.File.Exists(PackagePath());
        return View();
    }

    [HttpGet]
    public IActionResult Windows()
    {
        var package = PackagePath();
        if (!System.IO.File.Exists(package))
            return NotFound("The Windows client has not been published by library staff yet.");

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(package, "application/zip", PackageFileName, enableRangeProcessing: false);
    }
}

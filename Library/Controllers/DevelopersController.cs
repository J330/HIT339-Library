using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace S379658_HIT339_Assignment2_Library.Controllers;

[AllowAnonymous]
public sealed class DevelopersController : Controller
{
    public IActionResult Index() => View();
}

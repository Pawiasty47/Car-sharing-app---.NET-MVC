using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;
    private readonly UserManager<User> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        AppDbContext db,
        UserManager<User> userManager)
    {
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    // STRONA GŁÓWNA
    public async Task<IActionResult> Index()
    {
        var vm = new HomeIndexViewModel
        {
            UsersCount = await _db.Users.CountAsync(),
            RidesCount = await _db.OfferedRides.CountAsync(),
            IsLoggedIn = User.Identity?.IsAuthenticated ?? false
        };

        if (vm.IsLoggedIn)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            vm.IsAdmin = roles.Contains("Admin");
            vm.IsDriver = await _db.DriverProfiles.AnyAsync(d => d.UserId == user.Id);
            vm.IsPassenger = roles.Contains("Passenger") && !vm.IsDriver;
        }

        return View(vm);
    }

    // POLITYKA PRYWATNOŚCI
    public IActionResult Privacy()
    {
        return View();
    }

    // OBSŁUGA BŁĘDÓW
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}

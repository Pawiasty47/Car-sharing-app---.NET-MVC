using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
    public class DriverProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public DriverProfileController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🚗 ZOSTAŃ KIEROWCĄ
        [HttpGet]
        public async Task<IActionResult> BecomeDriver()
        {
            var user = await _userManager.GetUserAsync(User);

            var exists = await _context.DriverProfiles
                .AnyAsync(d => d.UserId == user.Id);

            if (!exists)
            {
                var driver = new DriverProfile
                {
                    UserId = user.Id,
                    IsVerified = false,
                    Rating = 0,
                    CompletedRidesCount = 0
                };

                _context.DriverProfiles.Add(driver);
                await _context.SaveChangesAsync();
            }

            // Wyraźnie przekierowujemy do Index akcji tego kontrolera
            return RedirectToAction(nameof(Index), "DriverProfile");
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = await _context.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (profile == null)
                return RedirectToAction(nameof(BecomeDriver));

            return View(profile);
        }
    }
}

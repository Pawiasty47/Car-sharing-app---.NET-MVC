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

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _context.DriverApplications
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.ApplicationDate)
                .FirstOrDefaultAsync();

            ViewBag.ApplicationStatus = application?.Status;

            var profile = await _context.DriverProfiles
                .Include(d => d.User)
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (profile == null)
                return RedirectToAction(nameof(BecomeDriver));

            // 🔥 KROK 1 — ZAKOŃCZONE PRZEJAZDY JAKO KIEROWCA
            var completedRides = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .Include(r => r.Bookings)
                .Where(r =>
                    r.DriverId == user.Id &&
                    r.Status == RideStatus.Completed)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            profile.CompletedRidesCount = completedRides.Count;
            ViewBag.CompletedRides = completedRides;

            ViewBag.TotalEarnings = completedRides
                .Sum(r => r.PricePerSeat * r.Bookings.Count);

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = await _context.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (profile == null)
                return RedirectToAction(nameof(Index));

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DriverProfile model, IFormFile? licenseImage)
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = await _context.DriverProfiles
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (profile == null)
                return RedirectToAction(nameof(Index));

            if (licenseImage != null && licenseImage.Length > 0)
            {
                var uploadsPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/licenses");

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(licenseImage.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await licenseImage.CopyToAsync(stream);

                profile.DrivingLicenseImageUrl = $"/uploads/licenses/{fileName}";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

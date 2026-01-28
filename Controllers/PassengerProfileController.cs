using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
    public class PassengerProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public PassengerProfileController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Błąd: Nie jesteś zalogowana.");

            var profile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var isDriver = await _context.DriverProfiles
                .AnyAsync(d => d.UserId == user.Id);

            if (profile == null)
            {
                profile = new PassengerProfile { UserId = user.Id };
                _context.PassengerProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            var driverProfile = isDriver
                ? await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == user.Id)
                : null;

            // 🔥 KROK 1 — ZAKOŃCZONE PRZEJAZDY JAKO PASAŻER
            var completedBookings = await _context.Bookings
                .Include(b => b.Ride)
                    .ThenInclude(r => r.Driver)
                        .ThenInclude(d => d.User)
                .Include(b => b.Ride.StartLocation)
                .Include(b => b.Ride.EndLocation)
                .Where(b =>
                    b.PassengerUserId == user.Id &&
                    b.Status == BookingStatus.Completed)
                .OrderByDescending(b => b.Ride.DepartureTime)
                .ToListAsync();

            ViewBag.CompletedBookings = completedBookings;

            var model = new PassengerProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ExistingProfilePicture = profile.ProfilePicture,

                PrefersNonSmoking = profile.PrefersNonSmoking,
                PrefersQuietRide = profile.PrefersQuietRide,
                PrefersMusic = profile.PrefersMusic,
                AcceptsPets = profile.AcceptsPets,
                AcceptsEating = profile.AcceptsEating,

                IsDriver = isDriver,

                PassengerRating = profile.Rating,
                DriverRating = driverProfile?.Rating
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new PassengerProfile { UserId = user.Id };
                _context.PassengerProfiles.Add(profile);
                await _context.SaveChangesAsync();
            }

            var model = new PassengerProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ExistingProfilePicture = profile.ProfilePicture,

                PrefersNonSmoking = profile.PrefersNonSmoking,
                PrefersQuietRide = profile.PrefersQuietRide,
                PrefersMusic = profile.PrefersMusic,
                AcceptsPets = profile.AcceptsPets,
                AcceptsEating = profile.AcceptsEating
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PassengerProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.PassengerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedUserName = model.Email.ToUpper();
                user.NormalizedEmail = model.Email.ToUpper();
            }

            await _userManager.UpdateAsync(user);

            if (profile != null)
            {
                profile.PrefersNonSmoking = model.PrefersNonSmoking;
                profile.PrefersQuietRide = model.PrefersQuietRide;
                profile.PrefersMusic = model.PrefersMusic;
                profile.AcceptsPets = model.AcceptsPets;
                profile.AcceptsEating = model.AcceptsEating;

                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await model.ProfilePictureFile.CopyToAsync(ms);
                    profile.ProfilePicture = ms.ToArray();
                }

                _context.Update(profile);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Zapisano zmiany!";
            return RedirectToAction(nameof(Index));
        }
    }
}

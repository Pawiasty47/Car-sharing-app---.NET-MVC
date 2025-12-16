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

        // Widok profilu
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Błąd: Nie jesteś zalogowana.");

            var profile = await _context.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

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
                ExistingProfilePicture = profile.ProfilePicture, // Przekazujemy zdjęcie do widoku

                // Mapujemy wszystkie preferencje
                PrefersNonSmoking = profile.PrefersNonSmoking,
                PrefersQuietRide = profile.PrefersQuietRide,
                PrefersMusic = profile.PrefersMusic,
                AcceptsPets = profile.AcceptsPets,
                AcceptsEating = profile.AcceptsEating
            };

            return View(model);
        }

        // Formularz edycji
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

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
                ExistingProfilePicture = profile.ProfilePicture, // Żebyśmy widzieli co mamy

                PrefersNonSmoking = profile.PrefersNonSmoking,
                PrefersQuietRide = profile.PrefersQuietRide,
                PrefersMusic = profile.PrefersMusic,
                AcceptsPets = profile.AcceptsPets,
                AcceptsEating = profile.AcceptsEating
            };

            return View(model);
        }

        // Zapis danych
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PassengerProfileViewModel model)
        {
            // Walidacja modelu (pomijamy walidację zdjęcia, bo jest opcjonalne)
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            // 1. Zapis Usera
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

            // 2. Zapis Profilu
            if (profile != null)
            {
                // Preferencje
                profile.PrefersNonSmoking = model.PrefersNonSmoking;
                profile.PrefersQuietRide = model.PrefersQuietRide;
                profile.PrefersMusic = model.PrefersMusic;
                profile.AcceptsPets = model.AcceptsPets;
                profile.AcceptsEating = model.AcceptsEating;

                // Zdjęcie
                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.ProfilePictureFile.CopyToAsync(memoryStream);
                        profile.ProfilePicture = memoryStream.ToArray(); // Konwersja pliku na bazę danych
                    }
                }

                _context.Update(profile);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Zapisano zmiany!";
            return RedirectToAction(nameof(Index));
        }
    }
}
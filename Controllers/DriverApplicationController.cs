using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models; // UWAGA: Usunąłem "using projekt_zespołowy.Data" bo to powodowało błąd
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
    public class DriverApplicationController : Controller
    {
        private readonly AppDbContext _context; // To zadziała, bo AppDbContext jest globalny
        private readonly UserManager<User> _userManager;

        public DriverApplicationController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var existingApp = await _context.DriverApplications
                .Include(d => d.Vehicle)
                .OrderByDescending(d => d.ApplicationDate)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (existingApp != null)
            {
                return View("Status", existingApp);
            }

            var vehicles = await _context.Vehicles.Where(v => v.OwnerId == user.Id).ToListAsync();

            if (!vehicles.Any())
            {
                TempData["Error"] = "Musisz dodać najpierw samochód, aby zostać kierowcą!";
                return RedirectToAction("Create", "Vehicle");
            }

            var model = new DriverApplicationViewModel
            {
                // Ustawiamy domyślną datę: Dzisiaj minus 18 lat
                // Dzięki temu kalendarz otworzy się np. na roku 2006, a nie 0001
                DateOfBirth = DateTime.Today.AddYears(-18),

                MyVehicles = vehicles.Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.Make} {v.Model} ({v.RegistrationNumber})"
                }).ToList()
            };

            return View("Apply", model);
        }

        [HttpPost]
        public async Task<IActionResult> Apply(DriverApplicationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            // Walidacja wieku
            var today = DateTime.Today;
            var age = today.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth.Date > today.AddYears(-age)) age--;

            if (age < 18)
            {
                ModelState.AddModelError("DateOfBirth", "Musisz mieć ukończone 18 lat, aby zostać kierowcą.");
            }

            if (model.DateOfBirth > DateTime.Now)
            {
                ModelState.AddModelError("DateOfBirth", "Data urodzenia nie może być z przyszłości.");
            }

            // --- NOWA WALIDACJA: PESEL vs Data Urodzenia (DODANO TUTAJ) ---
            if (!string.IsNullOrEmpty(model.PESEL) && model.PESEL.Length == 11)
            {
                // 1. Wyciągamy rok (dwie ostatnie cyfry)
                string yearStr = model.DateOfBirth.ToString("yy");

                // 2. Wyciągamy miesiąc (z uwzględnieniem roku 2000+)
                int month = model.DateOfBirth.Month;
                if (model.DateOfBirth.Year >= 2000)
                {
                    month += 20; // Dla roczników 2000+ dodaje się 20 do miesiąca
                }
                string monthStr = month.ToString("00"); // Np. 5 zamienia na "05", a 25 na "25"

                // 3. Wyciągamy dzień
                string dayStr = model.DateOfBirth.Day.ToString("00");

                // 4. Sklejamy oczekiwany początek PESELu
                string expectedStart = yearStr + monthStr + dayStr;

                // 5. Sprawdzamy czy PESEL zaczyna się od tego ciągu
                if (!model.PESEL.StartsWith(expectedStart))
                {
                    ModelState.AddModelError("PESEL", $"PESEL nie zgadza się z podaną datą urodzenia. Dla daty {model.DateOfBirth:dd.MM.yyyy} PESEL powinien zaczynać się od: {expectedStart}");
                }
            }
            // -------------------------------------------------------------

            if (!ModelState.IsValid)
            {
                var vehicles = await _context.Vehicles.Where(v => v.OwnerId == user.Id).ToListAsync();
                model.MyVehicles = vehicles.Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = $"{v.Make} {v.Model}"
                }).ToList();
                return View("Apply", model);
            }

            var application = new DriverApplication
            {
                UserId = user.Id,
                FirstName = model.FirstName,
                SecondName = model.SecondName,
                LastName = model.LastName,

                IdDocumentNumber = model.IdDocumentNumber,
                DriverLicenseNumber = model.DriverLicenseNumber,
                LicenseCategories = model.LicenseCategories,
                DateOfBirth = model.DateOfBirth,
                PESEL = model.PESEL,

                VehicleId = model.SelectedVehicleId,

                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.Now
            };

            _context.DriverApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Wniosek został wysłany!";
            return RedirectToAction("Index");
        }
    }
}
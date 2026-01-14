using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    [Authorize] // Wymagamy logowania
    public class VehicleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public VehicleController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Vehicle
        // ZMIANA: Wyświetla TYLKO samochody zalogowanego użytkownika (chyba że jest Adminem)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            List<Vehicle> vehicles;

            if (User.IsInRole("Admin"))
            {
                // Admin widzi wszystko
                vehicles = await _context.Vehicles
                    .Include(v => v.Owner)
                    .ToListAsync();
            }
            else
            {
                // Zwykły użytkownik widzi TYLKO swoje
                vehicles = await _context.Vehicles
                    .Include(v => v.Owner)
                    .Where(v => v.OwnerId == user.Id) // <--- FILTROWANIE
                    .ToListAsync();
            }

            // Ustawiamy flagę, żeby widok wiedział, czy pokazać przyciski edycji
            // (Dla właściciela zawsze true w tym widoku)
            ViewBag.IsMyVehiclesMode = true;

            return View(vehicles);
        }

        // Ta metoda jest teraz w sumie taka sama jak Index, ale możemy ją zostawić dla kompatybilności
        public async Task<IActionResult> MyVehicles()
        {
            return RedirectToAction(nameof(Index));
        }

        // GET: Vehicle/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var v = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) return NotFound();

            return View(v);
        }

        // GET: Vehicle/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Vehicle/Create
        // ZMIANA: Automatyczne przypisanie właściciela
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddVehicleViewModel vehicle)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Automatycznie przypisujemy OwnerId do zalogowanego użytkownika
            var ownerId = user.Id;

            // Usuwamy błędy walidacji dla OwnerId, bo my je ustawiamy
            ModelState.Remove("OwnerId");

            if (!ModelState.IsValid)
            {
                return View(vehicle);
            }

            var v = new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = vehicle.Make,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                SeatsTotal = vehicle.SeatsTotal,
                SeatsAvailable = vehicle.SeatsTotal - 1, // Zakładamy, że kierowca zajmuje 1 miejsce
                Color = vehicle.Color,
                OwnerId = ownerId // <--- PRZYPISANIE
            };

            _context.Vehicles.Add(v);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd został dodany do Twojego garażu!";
            return RedirectToAction("Index");
        }

        // GET: Vehicle/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // ZABEZPIECZENIE: Nie pozwalamy edytować cudzych aut (chyba że Admin)
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id)
            {
                return Forbid();
            }

            var model = new AddVehicleViewModel
            {
                Make = v.Make,
                Model = v.Model,
                RegistrationNumber = v.RegistrationNumber,
                SeatsTotal = v.SeatsTotal,
                Color = v.Color,
                OwnerId = v.OwnerId
            };

            return View(model);
        }

        // POST: Vehicle/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddVehicleViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id) return Forbid();

            v.Make = model.Make;
            v.Model = model.Model;
            v.RegistrationNumber = model.RegistrationNumber;
            v.SeatsTotal = model.SeatsTotal;
            v.Color = model.Color;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd zaktualizowany!";
            return RedirectToAction("Index");
        }

        // GET: Vehicle/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            // Zabezpieczenie podglądu usuwania
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id) return Forbid();

            return View(v);
        }

        // POST: Vehicle/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && vehicle.OwnerId != user.Id) return Forbid();

            // Najpierw usuwamy oferty przejazdów powiązane z tym autem
            var rides = _context.OfferedRides.Where(r => r.VehicleId == id);
            _context.OfferedRides.RemoveRange(rides);

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction(nameof(Index));
        }
    }
}
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
        // Wyświetla wszystkie samochody (tryb ogólny)
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Owner)
                .ToListAsync();

            // WAŻNE: Informujemy widok, że to jest lista ogólna
            // (Ukryje przyciski edycji dla nie-właścicieli i nie-adminów)
            ViewBag.IsMyVehiclesMode = false;

            return View(vehicles);
        }

        // GET: Vehicle/MyVehicles
        // Wyświetla tylko samochody zalogowanego użytkownika (tryb zarządzania)
        public async Task<IActionResult> MyVehicles()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId == user.Id)
                .ToListAsync();

            // WAŻNE: Informujemy widok, że to jest zakładka "Moje samochody"
            // (Pokaże przyciski edycji/usuwania)
            ViewBag.IsMyVehiclesMode = true;

            // Używamy tego samego widoku co Index
            return View("Index", vehicles);
        }

        // GET: Vehicle/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var v = await _context.Vehicles
                .Include(v => v.Owner) // Warto dołączyć dane właściciela
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddVehicleViewModel vehicle)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            Guid ownerId;
            if (!vehicle.OwnerId.HasValue || vehicle.OwnerId.Value == Guid.Empty)
            {
                ownerId = user.Id;
            }
            else
            {
                ownerId = vehicle.OwnerId.Value;
            }

            var v = new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = vehicle.Make,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                SeatsTotal = vehicle.SeatsTotal,
                SeatsAvailable = vehicle.SeatsTotal - 1,
                Color = vehicle.Color,
                OwnerId = ownerId
            };

            _context.Vehicles.Add(v);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd został dodany!";
            return RedirectToAction("Index");
        }

        // GET: Vehicle/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            // Opcjonalne zabezpieczenie: Sprawdź czy edytujący to właściciel lub Admin
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id)
            {
                return Forbid(); // Zabroń edycji cudzego auta przez URL
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
            if (!ModelState.IsValid)
                return View(model);

            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            // Opcjonalne zabezpieczenie po stronie POST
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id)
            {
                return Forbid();
            }

            v.Make = model.Make;
            v.Model = model.Model;
            v.RegistrationNumber = model.RegistrationNumber;
            v.SeatsTotal = model.SeatsTotal;
            v.Color = model.Color;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd zaktualizowany!";
            // Jeśli edytowaliśmy w trybie "Moje pojazdy", warto tam wrócić,
            // ale Index jest bezpiecznym domyślnym wyborem.
            return RedirectToAction("Index");
        }

        // GET: Vehicle/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            return View(v);
        }

        // POST: Vehicle/Delete/5
        [HttpPost, ActionName("Delete")] // WAŻNE: To pozwala formularzowi asp-action="Delete" znaleźć tę metodę
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            // Zabezpieczenie usuwania
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && vehicle.OwnerId != user.Id)
            {
                return Forbid();
            }

            // Usuwanie powiązanych ofert przejazdów
            var rides = _context.OfferedRides.Where(r => r.VehicleId == id);
            _context.OfferedRides.RemoveRange(rides);

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction(nameof(Index));
        }
    }
}
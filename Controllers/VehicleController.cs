using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    public class VehicleController : Controller
    {
        private readonly AppDbContext _context;

        public VehicleController(AppDbContext context)
        {
            _context = context;
        }

        // --- ZADANIE 1 i 2 ---
        // 1. Pobierasz listę z bazy (_context.Vehicles...)
        // 2. Przekazujesz do widoku (return View(vehicles))
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Owner) // Dociągamy właściciela, żeby mieć komplet danych
                .ToListAsync();

            return View(vehicles);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var v = await _context.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();

            return View(v);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddVehicleViewModel vehicle)
        {
            if (!ModelState.IsValid)
                return View(vehicle);

            var v = new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = vehicle.Make,
                Model = vehicle.Model,
                RegistrationNumber = vehicle.RegistrationNumber,
                SeatsTotal = vehicle.SeatsTotal,
                SeatsAvailable = vehicle.SeatsTotal - 1, // Zakładamy, że kierowca zajmuje jedno miejsce
                Color = vehicle.Color,
                OwnerId = vehicle.OwnerId // <--- TUTAJ JEST KLUCZOWA ZMIANA
            };

            _context.Vehicles.Add(v);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd został dodany!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            var model = new AddVehicleViewModel
            {
                Make = v.Make,
                Model = v.Model,
                RegistrationNumber = v.RegistrationNumber,
                SeatsTotal = v.SeatsTotal,
                Color = v.Color,
                OwnerId = v.OwnerId // Wczytujemy istniejącego właściciela do formularza
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddVehicleViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            v.Make = model.Make;
            v.Model = model.Model;
            v.RegistrationNumber = model.RegistrationNumber;
            v.SeatsTotal = model.SeatsTotal;
            v.Color = model.Color;
            // v.OwnerId = model.OwnerId; // Opcjonalnie: odkomentuj, jeśli chcesz pozwalać na zmianę ID właściciela przy edycji

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd zaktualizowany!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            return View(v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            _context.Vehicles.Remove(v);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction("Index");
        }
    }
}
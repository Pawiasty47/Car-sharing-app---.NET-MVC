using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    [Authorize] // Wymagamy logowania, jeśli chcesz zabezpieczyć samochody
    public class VehicleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public VehicleController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Wyświetla wszystkie samochody
        public async Task<IActionResult> Index()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Owner)
                .ToListAsync();

            return View(vehicles);
        }

        // Wyświetla tylko samochody zalogowanego użytkownika
        public async Task<IActionResult> MyVehicles()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId == user.Id)
                .ToListAsync();

            return View("Index", vehicles); // Korzystamy z tego samego widoku co Index
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

            // Pobranie zalogowanego użytkownika
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Wybór OwnerId: ręcznie podany lub domyślnie zalogowany użytkownik
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
                OwnerId = v.OwnerId
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
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var rides = _context.OfferedRides.Where(r => r.VehicleId == id);
            _context.OfferedRides.RemoveRange(rides);

            _context.Vehicles.Remove(vehicle);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd oraz powiązane przejazdy zostały usunięte!";
            return RedirectToAction(nameof(Index));
        }
    }
}

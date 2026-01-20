using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
    public class VehicleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // NOWE: Do zapisu plików

        public VehicleController(AppDbContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Vehicle
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            List<Vehicle> vehicles;

            if (User.IsInRole("Admin"))
            {
                vehicles = await _context.Vehicles.Include(v => v.Owner).ToListAsync();
            }
            else
            {
                vehicles = await _context.Vehicles.Include(v => v.Owner).Where(v => v.OwnerId == user.Id).ToListAsync();
            }

            ViewBag.IsMyVehiclesMode = true;
            return View(vehicles);
        }

        public async Task<IActionResult> MyVehicles() => RedirectToAction(nameof(Index));

        public async Task<IActionResult> Details(Guid id)
        {
            // POPRAWKA: Usunięto .ThenInclude(u => u.User), ponieważ Owner to już jest User
            var v = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) return NotFound();
            return View(v);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddVehicleViewModel vehicle)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ownerId = user.Id;
            // Admin może przypisać właściciela ręcznie
            if (User.IsInRole("Admin") && vehicle.OwnerId.HasValue)
            {
                ownerId = vehicle.OwnerId.Value;
            }
            ModelState.Remove("OwnerId");

            if (!ModelState.IsValid) return View(vehicle);

            // --- ZAPIS ZDJĘCIA ---
            string? uniqueFileName = null;
            if (vehicle.VehiclePhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vehicles");
                Directory.CreateDirectory(uploadsFolder);
                uniqueFileName = Guid.NewGuid().ToString() + "_" + vehicle.VehiclePhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vehicle.VehiclePhoto.CopyToAsync(fileStream);
                }
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
                OwnerId = ownerId,
                ImageUrl = uniqueFileName
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

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id) return Forbid();

            var model = new AddVehicleViewModel
            {
                Make = v.Make,
                Model = v.Model,
                RegistrationNumber = v.RegistrationNumber,
                SeatsTotal = v.SeatsTotal,
                Color = v.Color,
                OwnerId = v.OwnerId,
                ExistingImageUrl = v.ImageUrl // Przekazujemy stare zdjęcie
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddVehicleViewModel model)
        {
            if (model.VehiclePhoto == null) ModelState.Remove("VehiclePhoto");
            ModelState.Remove("OwnerId");

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

            // --- ZMIANA ZDJĘCIA ---
            if (model.VehiclePhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vehicles");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.VehiclePhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.VehiclePhoto.CopyToAsync(fileStream);
                }
                v.ImageUrl = uniqueFileName;
            }

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var rides = _context.OfferedRides.Where(r => r.VehicleId == id);
            _context.OfferedRides.RemoveRange(rides);
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction(nameof(Index));
        }
    }
}
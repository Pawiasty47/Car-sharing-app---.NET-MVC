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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VehicleController(AppDbContext context, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // --- INDEX ---
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            List<Vehicle> vehicles;
            if (User.IsInRole("Admin"))
                vehicles = await _context.Vehicles.Include(v => v.Owner).ToListAsync();
            else
                vehicles = await _context.Vehicles.Include(v => v.Owner).Where(v => v.OwnerId == user.Id).ToListAsync();

            ViewBag.IsMyVehiclesMode = true;
            return View(vehicles);
        }

        // --- DETAILS ---
        public async Task<IActionResult> Details(Guid id)
        {
            var v = await _context.Vehicles.Include(v => v.Owner).FirstOrDefaultAsync(x => x.Id == id);
            if (v == null) return NotFound();
            return View(v);
        }

        // --- CREATE ---
        public IActionResult Create(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddVehicleViewModel vehicle, string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var ownerId = user.Id;
            if (User.IsInRole("Admin") && vehicle.OwnerId.HasValue) ownerId = vehicle.OwnerId.Value;
            ModelState.Remove("OwnerId");

            if (!ModelState.IsValid) return View(vehicle);

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
                RegistrationNumber = vehicle.RegistrationNumber.ToUpper(),
                SeatsTotal = vehicle.SeatsTotal,
                SeatsAvailable = vehicle.SeatsTotal - 1,
                Color = vehicle.Color,
                OwnerId = ownerId,
                ImageUrl = uniqueFileName
            };
            _context.Vehicles.Add(v);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pojazd dodany!";
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }

        // --- EDIT (GET) ---
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
                ExistingImageUrl = v.ImageUrl
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddVehicleViewModel model)
        {
            var v = await _context.Vehicles.FindAsync(id);
            if (v == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("Admin") && v.OwnerId != user.Id) return Forbid();

            string formMake = Request.Form["Make"];
            string formModel = Request.Form["Model"];
            string formReg = Request.Form["RegistrationNumber"];
            string formColor = Request.Form["Color"];
            string formSeats = Request.Form["SeatsTotal"];

            if (!string.IsNullOrEmpty(formMake)) v.Make = formMake;
            else if (!string.IsNullOrEmpty(model.Make)) v.Make = model.Make;

            if (!string.IsNullOrEmpty(formModel)) v.Model = formModel;
            else if (!string.IsNullOrEmpty(model.Model)) v.Model = model.Model;

            if (!string.IsNullOrEmpty(formReg)) v.RegistrationNumber = formReg.ToUpper();
            else if (!string.IsNullOrEmpty(model.RegistrationNumber)) v.RegistrationNumber = model.RegistrationNumber.ToUpper();

            if (!string.IsNullOrEmpty(formColor)) v.Color = formColor;
            else if (!string.IsNullOrEmpty(model.Color)) v.Color = model.Color;

            int seats = 0;
            if (!string.IsNullOrEmpty(formSeats) && int.TryParse(formSeats, out int s)) seats = s;
            else seats = model.SeatsTotal;

            if (seats > 0)
            {
                v.SeatsTotal = seats;
                v.SeatsAvailable = seats - 1;
            }

            
            IFormFile? fileToSave = model.VehiclePhoto;

            if (fileToSave == null && Request.Form.Files.Count > 0)
            {
                fileToSave = Request.Form.Files["VehiclePhoto"];
            }

            if (fileToSave != null && fileToSave.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vehicles");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(fileToSave.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileToSave.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(v.ImageUrl))
                {
                    string oldPath = Path.Combine(uploadsFolder, v.ImageUrl);
                    if (System.IO.File.Exists(oldPath))
                    {
                        try { System.IO.File.Delete(oldPath); } catch { } // Ignorujemy błędy przy usuwaniu
                    }
                }
                v.ImageUrl = uniqueFileName;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pojazd zaktualizowany!";
            return RedirectToAction("Index");
        }

        // --- DELETE ---
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

            if (!string.IsNullOrEmpty(vehicle.ImageUrl))
            {
                string path = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vehicles", vehicle.ImageUrl);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Pojazd usunięty!";
            return RedirectToAction(nameof(Index));
        }
    }
}
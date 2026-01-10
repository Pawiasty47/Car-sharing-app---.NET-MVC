using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    public class RidesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public RidesController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. LISTA PRZEJAZDÓW (Z wyszukiwaniem)
        public async Task<IActionResult> Index(string searchFrom, string searchTo, DateTime? searchDate)
        {
            var query = _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchFrom))
            {
                query = query.Where(r => r.StartLocation.City.Contains(searchFrom) || r.StartLocation.Name.Contains(searchFrom));
            }

            if (!string.IsNullOrEmpty(searchTo))
            {
                query = query.Where(r => r.EndLocation.City.Contains(searchTo) || r.EndLocation.Name.Contains(searchTo));
            }

            if (searchDate.HasValue)
            {
                query = query.Where(r => r.DepartureTime.Date == searchDate.Value.Date);
            }

            var rides = await query
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewData["CurrentFilterFrom"] = searchFrom;
            ViewData["CurrentFilterTo"] = searchTo;
            ViewData["CurrentFilterDate"] = searchDate?.ToString("yyyy-MM-dd");

            ViewBag.IsMyRidesMode = false;

            return View(rides);
        }

        // 1a. MOJE PRZEJAZDY
        [Authorize]
        public async Task<IActionResult> MyRides()
        {
            var userId = _userManager.GetUserId(User);

            var myRides = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .Where(r => r.DriverId.ToString() == userId)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewBag.IsMyRidesMode = true;

            return View("Index", myRides);
        }

        // 2. SZCZEGÓŁY
        public async Task<IActionResult> Details(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User) // Dane kierowcy
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            // NOWOŚĆ: Pobieramy listę rezerwacji dla tego przejazdu
            // Dołączamy (Include) dane pasażera, żeby wyświetlić imię i nazwisko
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Where(b => b.RideId == id)
                .ToListAsync();

            // Przekazujemy listę do widoku za pomocą ViewBag
            ViewBag.Bookings = bookings;

            return View(ride);
        }

        // 3. TWORZENIE (GET)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId.ToString() == userId)
                .ToListAsync();

            if (!vehicles.Any())
            {
                TempData["Error"] = "Musisz dodać pojazd, zanim dodasz przejazd!";
                return RedirectToAction("Create", "Vehicle");
            }

            var model = new AddRideViewModel
            {
                AvailableVehicles = vehicles,
                DepartureTime = DateTime.Now.AddHours(1)
            };
            return View(model);
        }

        // 4. TWORZENIE (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddRideViewModel model)
        {
            ModelState.Remove("AvailableVehicles");
            ModelState.Remove("StartLocation.Latitude");
            ModelState.Remove("StartLocation.Longtitude");
            ModelState.Remove("EndLocation.Latitude");
            ModelState.Remove("EndLocation.Longtitude");

            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles
                    .Where(v => v.OwnerId.ToString() == userId).ToListAsync();
                return View(model);
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.Id == model.SelectedVehicleId);

            if (vehicle == null)
            {
                ModelState.AddModelError("", "Wybrany pojazd nie istnieje.");
                model.AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == userId).ToListAsync();
                return View(model);
            }

            if (model.DepartureTime <= DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "Data wyjazdu musi być w przyszłości.");
            }

            if (model.ArrivalTime.HasValue && model.ArrivalTime <= model.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Data przyjazdu musi być późniejsza niż data wyjazdu.");
            }

            int maxPassengerSeats = vehicle.SeatsTotal > 0 ? vehicle.SeatsTotal - 1 : 0;
            if (model.SeatsOffered > maxPassengerSeats)
            {
                ModelState.AddModelError("SeatsOffered",
                    $"Pojazd ma {vehicle.SeatsTotal} miejsc. Max pasażerów to {maxPassengerSeats}.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == userId).ToListAsync();
                return View(model);
            }

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId.ToString() == userId);

            if (driverProfile == null)
            {
                return RedirectToAction("BecomeDriver", "DriverProfile");
            }

            var startLoc = new LocationPoint
            {
                Id = Guid.NewGuid(),
                Name = model.StartLocation.Name,
                City = model.StartLocation.City,
                Address = model.StartLocation.Address,
                Latitude = 52.2297,
                Longtitude = 21.0122
            };

            var endLoc = new LocationPoint
            {
                Id = Guid.NewGuid(),
                Name = model.EndLocation.Name,
                City = model.EndLocation.City,
                Address = model.EndLocation.Address,
                Latitude = 50.0647,
                Longtitude = 19.9450
            };

            var ride = new OfferedRide
            {
                Id = Guid.NewGuid(),
                VehicleId = model.SelectedVehicleId,
                DriverId = driverProfile.UserId,
                StartLocation = startLoc,
                EndLocation = endLoc,
                DepartureTime = model.DepartureTime,
                ArrivalTime = model.ArrivalTime,
                SeatsOffered = model.SeatsOffered,
                SeatsTaken = 0,
                PricePerSeat = model.PricePerSeat,
                IsFlexiblePrice = model.IsFlexiblePrice,
                Notes = model.Notes,
                Status = RideStatus.Published
            };

            _context.OfferedRides.Add(ride);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pomyślnie dodano nowy przejazd!";

            // ZMIANA: Przekierowanie do Index (tak jak w VehicleController)
            return RedirectToAction(nameof(Index));
        }

        // 5. EDYCJA (GET)
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // Zabezpieczenie: Właściciel LUB Admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            var model = new AddRideViewModel
            {
                SelectedVehicleId = ride.VehicleId,
                StartLocation = new LocationInputModel
                {
                    Name = ride.StartLocation.Name,
                    Address = ride.StartLocation.Address,
                    City = ride.StartLocation.City,
                    Latitude = ride.StartLocation.Latitude,
                    Longtitude = ride.StartLocation.Longtitude
                },
                EndLocation = new LocationInputModel
                {
                    Name = ride.EndLocation.Name,
                    Address = ride.EndLocation.Address,
                    City = ride.EndLocation.City,
                    Latitude = ride.EndLocation.Latitude,
                    Longtitude = ride.EndLocation.Longtitude
                },
                DepartureTime = ride.DepartureTime,
                ArrivalTime = ride.ArrivalTime,
                SeatsOffered = ride.SeatsOffered,
                PricePerSeat = ride.PricePerSeat,
                IsFlexiblePrice = ride.IsFlexiblePrice,
                Notes = ride.Notes,
                AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == userId).ToListAsync()
            };

            return View(model);
        }

        // 6. EDYCJA (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddRideViewModel model)
        {
            ModelState.Remove("AvailableVehicles");
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == userId).ToListAsync();
                return View(model);
            }

            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            // Zabezpieczenie: Właściciel LUB Admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            ride.VehicleId = model.SelectedVehicleId;
            ride.DepartureTime = model.DepartureTime;
            ride.ArrivalTime = model.ArrivalTime;
            ride.SeatsOffered = model.SeatsOffered;
            ride.PricePerSeat = model.PricePerSeat;
            ride.IsFlexiblePrice = model.IsFlexiblePrice;
            ride.Notes = model.Notes;

            ride.StartLocation.Name = model.StartLocation.Name;
            ride.StartLocation.Address = model.StartLocation.Address;
            ride.StartLocation.City = model.StartLocation.City;

            ride.EndLocation.Name = model.EndLocation.Name;
            ride.EndLocation.Address = model.EndLocation.Address;
            ride.EndLocation.City = model.EndLocation.City;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd zaktualizowany!";

            // ZMIANA: Przekierowanie do Index (tak jak w VehicleController)
            return RedirectToAction(nameof(Index));
        }

        // 7. USUWANIE (GET)
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // Zabezpieczenie: Właściciel LUB Admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            return View(ride);
        }

        // POST: Rides/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ride = await _context.OfferedRides.FindAsync(id);
            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // Zabezpieczenie: Właściciel LUB Admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            _context.OfferedRides.Remove(ride);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd został usunięty!";

            // ZMIANA: Przekierowanie do Index (tak jak w VehicleController)
            // To naprawia problem z odświeżaniem listy po usunięciu
            return RedirectToAction(nameof(Index));
        }

    }
}
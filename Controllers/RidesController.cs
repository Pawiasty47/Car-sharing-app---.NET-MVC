using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    public class RidesController : Controller
    {
        private readonly AppDbContext _context;

        public RidesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LISTA PRZEJAZDÓW
        public async Task<IActionResult> Index()
        {
            var rides = await _context.OfferedRides
                .Include(r => r.Vehicle)        
                .Include(r => r.StartLocation) 
                .Include(r => r.EndLocation)    
                .OrderByDescending(r => r.DepartureTime) 
                .ToListAsync();

            return View(rides);
        }

        // 2. SZCZEGÓŁY
        public async Task<IActionResult> Details(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            return View(ride);
        }

        // 3. TWORZENIE (Formularz)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Pobieramy pojazdy, aby wypełnić listę rozwijaną w widoku
            var vehicles = await _context.Vehicles.ToListAsync();

            var model = new AddRideViewModel
            {
                AvailableVehicles = vehicles,
                DepartureTime = DateTime.Now.AddHours(1)
            };
            return View(model);
        }

        // 4. TWORZENIE (Zapis do bazy)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddRideViewModel model)
        {
            // 1. CZYSZCZENIE WALIDACJI (Techniczne)
            ModelState.Remove("AvailableVehicles");
            ModelState.Remove("StartLocation.Latitude");
            ModelState.Remove("StartLocation.Longtitude");
            ModelState.Remove("EndLocation.Latitude");
            ModelState.Remove("EndLocation.Longtitude");

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles.ToListAsync();
                return View(model);
            }

            // 2. POBRANIE POJAZDU
            var vehicle = await _context.Vehicles
                .Include(v => v.Owner)
                .FirstOrDefaultAsync(v => v.Id == model.SelectedVehicleId);

            if (vehicle == null)
            {
                ModelState.AddModelError("", "Wybrany pojazd nie istnieje.");
                model.AvailableVehicles = await _context.Vehicles.ToListAsync();
                return View(model);
            }


            // Sprawdzenie daty wyjazdu
            if (model.DepartureTime <= DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "Data wyjazdu musi być w przyszłości.");
            }

            // Sprawdzenie daty przyjazdu 
            if (model.ArrivalTime.HasValue && model.ArrivalTime <= model.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Data przyjazdu musi być późniejsza niż data wyjazdu.");
            }

            // Sprawdzenie liczby miejsc
            int maxPassengerSeats = vehicle.SeatsTotal > 0 ? vehicle.SeatsTotal - 1 : 0; 

            if (model.SeatsOffered > maxPassengerSeats)
            {
                ModelState.AddModelError("SeatsOffered",
                    $"Wybrany pojazd ({vehicle.Make} {vehicle.Model}) ma {vehicle.SeatsTotal} miejsc ogółem. " +
                    $"Po odliczeniu kierowcy możesz zabrać maksymalnie {maxPassengerSeats} pasażerów.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles.ToListAsync();
                return View(model);
            }



            // 3. AUTO-NAPRAWA KIEROWCY (jesli pojazd jest bez wlasciciela)
            if (vehicle.OwnerId == null)
            {
                var existingDriver = await _context.DriverProfiles.FirstOrDefaultAsync();
                if (existingDriver != null)
                {
                    vehicle.OwnerId = existingDriver.UserId;
                }
                else
                {
                    var newUser = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = "Jan",
                        LastName = "Kowalski",
                        Email = "jan@test.pl",
                        PhoneNumber = "123456789"
                    };
                    var newDriver = new DriverProfile
                    {
                        UserId = newUser.Id,
                        User = newUser,
                        IsVerified = true,
                        DrivingLicenseImageUrl = "brak.jpg",
                        CompletedRidesCount = 0,
                        Rating = 5.0
                    };
                    _context.Users.Add(newUser);
                    _context.DriverProfiles.Add(newDriver);
                    vehicle.OwnerId = newUser.Id;
                }
                await _context.SaveChangesAsync();
            }

            Guid driverId = vehicle.OwnerId.Value;

            // 4. TWORZENIE DANYCH (Lokalizacje + Przejazd)
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
                DriverId = driverId,
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

            // 5. ZAPIS
            _context.OfferedRides.Add(ride);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pomyślnie dodano nowy przejazd!";
            return RedirectToAction(nameof(Index));
        }

        // 5. EDYCJA (Formularz)
        public async Task<IActionResult> Edit(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

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
                AvailableVehicles = await _context.Vehicles.ToListAsync()
            };

            return View(model);
        }

        // 6. EDYCJA (Zapis zmian)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddRideViewModel model)
        {
            ModelState.Remove("AvailableVehicles");

            if (!ModelState.IsValid)
            {
                model.AvailableVehicles = await _context.Vehicles.ToListAsync();
                return View(model);
            }

            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            // Aktualizujemy proste pola
            ride.VehicleId = model.SelectedVehicleId;
            ride.DepartureTime = model.DepartureTime;
            ride.ArrivalTime = model.ArrivalTime;
            ride.SeatsOffered = model.SeatsOffered;
            ride.PricePerSeat = model.PricePerSeat;
            ride.IsFlexiblePrice = model.IsFlexiblePrice;
            ride.Notes = model.Notes;

            // Aktualizujemy dane lokalizacji (nadpisujemy istniejące rekordy)
            ride.StartLocation.Name = model.StartLocation.Name;
            ride.StartLocation.Address = model.StartLocation.Address;
            ride.StartLocation.City = model.StartLocation.City;
            ride.StartLocation.Latitude = model.StartLocation.Latitude;
            ride.StartLocation.Longtitude = model.StartLocation.Longtitude;

            ride.EndLocation.Name = model.EndLocation.Name;
            ride.EndLocation.Address = model.EndLocation.Address;
            ride.EndLocation.City = model.EndLocation.City;
            ride.EndLocation.Latitude = model.EndLocation.Latitude;
            ride.EndLocation.Longtitude = model.EndLocation.Longtitude;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd zaktualizowany!";
            return RedirectToAction(nameof(Index));
        }

        // 7. USUWANIE (Pytanie)
        public async Task<IActionResult> Delete(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();
            return View(ride);
        }

        // 8. USUWANIE (Potwierdzenie)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ride = await _context.OfferedRides.FindAsync(id);
            if (ride == null) return NotFound();

            _context.OfferedRides.Remove(ride);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd usunięty!";
            return RedirectToAction(nameof(Index));
        }
    }
}
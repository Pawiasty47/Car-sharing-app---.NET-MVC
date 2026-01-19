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

        // Lista przejazdów (wyszukiwanie)
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

            // Pokazujemy w publicznym wyszukiwaniu tylko aktywne przejazdy (Published lub InProgress).
            query = query.Where(r => r.Status == RideStatus.Published || r.Status == RideStatus.InProgress);

            var rides = await query
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewData["CurrentFilterFrom"] = searchFrom;
            ViewData["CurrentFilterTo"] = searchTo;
            ViewData["CurrentFilterDate"] = searchDate?.ToString("yyyy-MM-dd");

            ViewBag.IsMyRidesMode = false;

            return View(rides);
        }

        // Moje przejazdy
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

        // Dodane: Moje ukończone przejazdy
        [Authorize]
        public async Task<IActionResult> Completed()
        {
            var userId = _userManager.GetUserId(User);

            // Sprawdź, czy użytkownik ma profil kierowcy
            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId.ToString() == userId);
            if (driverProfile == null)
            {
                return Forbid(); // 403 dla użytkowników, którzy nie są kierowcami
            }

            var completedRides = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .Where(r => r.DriverId == driverProfile.UserId && r.Status == RideStatus.Completed)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewBag.IsMyRidesMode = true;
            return View("Completed", completedRides);
        }

        // Szczegóły
        public async Task<IActionResult> Details(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User) // kierowca
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Where(b => b.RideId == id)
                .ToListAsync();

            ViewBag.Bookings = bookings;

            // DODANE: pobieramy profile pasażerów aby mieć ich oceny i przekazujemy do widoku
            var passengerUserIds = bookings.Select(b => b.PassengerUserId).Distinct().ToList();
            var passengerProfiles = await _context.PassengerProfiles
                .Where(p => passengerUserIds.Contains(p.UserId))
                .ToListAsync();

            var ratingsDict = passengerProfiles.ToDictionary(p => p.UserId, p => p.Rating);
            ViewBag.PassengerRatings = ratingsDict;

            return View(ride);
        }

        // Create (GET)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var vehicles = await _context.Vehicles
                .Where(v => v.OwnerId.ToString() == userId)
                .ToListAsync();

            // Jeśli brak pojazdów — nie przekierowujemy. Zwracamy formularz przejazdu z pustą listą i komunikatem.
            if (!vehicles.Any())
            {
                TempData["Error"] = "Nie masz jeszcze dodanych pojazdów. Wybierz pojazd po jego dodaniu lub dodaj pojazd w osobnym formularzu.";
                var emptyModel = new AddRideViewModel
                {
                    AvailableVehicles = vehicles, // pusta lista
                    DepartureTime = DateTime.Now.AddHours(1)
                };
                return View(emptyModel);
            }

            var model = new AddRideViewModel
            {
                AvailableVehicles = vehicles,
                DepartureTime = DateTime.Now.AddHours(1)
            };
            return View(model);
        }

        // Create (POST)
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

            return RedirectToAction(nameof(Index));
        }

        // Edit (GET)
        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // właściciel lub admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            // blokada gdy są rezerwacje
            var hasBookings = await _context.Bookings.AnyAsync(b => b.RideId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Nie można edytować przejazdu, ponieważ są już zapisani pasażerowie.";
                return RedirectToAction(nameof(Details), new { id });
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

            // potrzebne w widoku do ustawienia asp-route-id formularza
            ViewBag.RideId = ride.Id;

            return View(model);
        }

        // Edit (POST)
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
                ViewBag.RideId = id;
                return View(model);
            }

            var ride = await _context.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            // właściciel lub admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            // blokada gdy są rezerwacje
            var hasBookings = await _context.Bookings.AnyAsync(b => b.RideId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Nie można edytować przejazdu, ponieważ są już zapisani pasażerowie.";
                return RedirectToAction(nameof(Details), new { id });
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

        // Summary (GET) - widok podsumowania przejazdu
        [Authorize]
        public async Task<IActionResult> Summary(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser != null ? currentUser.Id.ToString() : null;

            // allow driver, admin or a passenger who had a booking
            var isDriver = currentUser != null && ride.DriverId == currentUser.Id;
            var isAdmin = User.IsInRole("Admin");

            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Where(b => b.RideId == id)
                .ToListAsync();

            var isPassenger = currentUser != null && bookings.Any(b => b.PassengerUserId == currentUser.Id);

            if (!isAdmin && !isDriver && !isPassenger)
            {
                return Forbid();
            }

            ViewBag.Bookings = bookings;

            // Pobierz recenzje wystawione przez aktualnego użytkownika dla tego przejazdu
            var myReviews = new Dictionary<Guid, Review>();
            if (currentUser != null)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.RideId == id && r.FromUserId == currentUser.Id)
                    .ToListAsync();

                myReviews = reviews.ToDictionary(r => r.ToUserId, r => r);
            }

            ViewBag.MyReviews = myReviews;
            ViewBag.IsPassenger = isPassenger;
            ViewBag.IsDriver = isDriver;

            return View("Summary", ride);
        }

        // POST: kierowca ocenia pasażera
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RatePassenger(Guid rideId, Guid passengerUserId, int rating, string? comment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);
            if (ride == null) return NotFound();

            // tylko kierowca (właściciel przejazdu) może oceniać pasażerów tego przejazdu
            if (!User.IsInRole("Admin") && ride.DriverId != currentUser.Id)
                return Forbid();

            // dopuszczamy ocenianie tylko po zakończeniu przejazdu
            if (ride.Status != RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Oceny można wystawić dopiero po zakończeniu przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            // sprawdź, czy pasażer był zapisany na ten przejazd
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.RideId == rideId && b.PassengerUserId == passengerUserId);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Ten użytkownik nie był pasażerem tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            // zapobiegaj wielokrotnemu ocenianiu tego samego pasażera przez tego samego użytkownika dla tego przejazdu
            var exists = await _context.Reviews.FirstOrDefaultAsync(r =>
                r.RideId == rideId && r.FromUserId == currentUser.Id && r.ToUserId == passengerUserId);

            if (exists != null)
            {
                TempData["ErrorMessage"] = "Już oceniłeś tego pasażera dla tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            var review = new Review
            {
                FromUserId = currentUser.Id,
                ToUserId = passengerUserId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment ?? string.Empty,
                CreateAt = DateTime.UtcNow,
                RideId = rideId
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Zaktualizuj średnią ocenę pasażera (PassengerProfile.Rating)
            var passengerProfile = await _context.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == passengerUserId);
            if (passengerProfile != null)
            {
                var all = await _context.Reviews.Where(r => r.ToUserId == passengerUserId).ToListAsync();
                passengerProfile.Rating = all.Any() ? all.Average(r => r.Rating) : 0.0;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Ocena zapisana.";
            return RedirectToAction("Summary", new { id = rideId });
        }

        // Complete (POST) - zakończenie przejazdu
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Bookings)
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            // właściciel lub admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            if (ride.Status == RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Przejazd już zakończony.";
                return RedirectToAction(nameof(Summary), new { id });
            }

            ride.Status = RideStatus.Completed;
            if (!ride.ArrivalTime.HasValue) ride.ArrivalTime = DateTime.Now;

            // Zmieniamy status rezerwacji na Completed
            var bookings = await _context.Bookings.Where(b => b.RideId == id).ToListAsync();
            foreach (var b in bookings)
            {
                b.Status = BookingStatus.Completed;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd zakończony.";
            return RedirectToAction(nameof(Summary), new { id });
        }

        // Delete (GET)
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
            // właściciel lub admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            // blokada gdy są rezerwacje
            var hasBookings = await _context.Bookings.AnyAsync(b => b.RideId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Nie można usunąć przejazdu, ponieważ są już zapisani pasażerowie.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(ride);
        }

        // Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ride = await _context.OfferedRides.FindAsync(id);
            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // właściciel lub admin
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            // blokada gdy są rezerwacje
            var hasBookings = await _context.Bookings.AnyAsync(b => b.RideId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "Nie można usunąć przejazdu, ponieważ są już zapisani pasażerowie.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.OfferedRides.Remove(ride);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd został usunięty!";

            return RedirectToAction(nameof(Index));
        }

        // POST: ocena kierowcy przez pasażera
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateDriver(Guid rideId, int rating, string? comment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);
            if (ride == null) return NotFound();

            // tylko pasażerowie zapisani na przejazd lub admin mogą oceniać kierowcę
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.RideId == rideId && b.PassengerUserId == currentUser.Id);
            if (booking == null && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Nie możesz ocenić tego kierowcy — nie byłeś pasażerem tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            // dopuszczamy ocenianie tylko po zakończeniu przejazdu
            if (ride.Status != RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Oceny można wystawić dopiero po zakończeniu przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            // zapobiegaj wielokrotnemu ocenianiu tego samego kierowcy przez tego samego pasażera dla tego przejazdu
            var exists = await _context.Reviews.FirstOrDefaultAsync(r =>
                r.RideId == rideId && r.FromUserId == currentUser.Id && r.ToUserId == ride.DriverId);

            if (exists != null)
            {
                TempData["ErrorMessage"] = "Już oceniłeś tego kierowcę dla tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            var review = new Review
            {
                FromUserId = currentUser.Id,
                ToUserId = ride.DriverId,
                Rating = Math.Clamp(rating, 1, 5),
                Comment = comment ?? string.Empty,
                CreateAt = DateTime.UtcNow,
                RideId = rideId
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Zaktualizuj średnią ocenę kierowcy (DriverProfile.Rating)
            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(p => p.UserId == ride.DriverId);
            if (driverProfile != null)
            {
                var all = await _context.Reviews.Where(r => r.ToUserId == ride.DriverId).ToListAsync();
                driverProfile.Rating = all.Any() ? all.Average(r => r.Rating) : 0.0;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Ocena zapisana.";
            return RedirectToAction("Summary", new { id = rideId });
        }

    }
}
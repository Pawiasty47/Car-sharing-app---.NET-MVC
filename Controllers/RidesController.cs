using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;
using System.Security.Claims;

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
        public async Task<IActionResult> Index(string searchFrom, string searchTo, DateTime? searchDate, bool onlyMine = false)
        {
            var query = _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User)
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

            query = query.Where(r =>
                (r.Status == RideStatus.Published || r.Status == RideStatus.InProgress)
                && r.DepartureTime >= DateTime.Now);

            // Jeśli użytkownik zaznaczył filtr "Tylko moje przejazdy" - przefiltruj po aktualnym kierowcy
            if (onlyMine)
            {
                var uid = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(uid))
                {
                    return Forbid();
                }

                query = query.Where(r => r.DriverId.ToString() == uid);
                ViewBag.IsMyRidesMode = true;
            }
            else
            {
                ViewBag.IsMyRidesMode = false;
            }

            var rides = await query
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewData["CurrentFilterFrom"] = searchFrom;
            ViewData["CurrentFilterTo"] = searchTo;
            ViewData["CurrentFilterDate"] = searchDate?.ToString("yyyy-MM-dd");

            // Pobierz przejazdy, na które aktualny użytkownik jest zapisany (żeby pokazać informację w widoku)
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var bookedIds = await _context.Bookings
                    .Where(b => b.PassengerUserId == currentUser.Id && b.Status != BookingStatus.Cancelled)
                    .Select(b => b.RideId)
                    .ToListAsync();
                ViewBag.BookedRideIds = bookedIds;
            }
            else
            {
                ViewBag.BookedRideIds = new List<Guid>();
            }

            return View(rides);
        }

        public async Task<IActionResult> Archival(string searchFrom, string searchTo, DateTime? searchDate, bool onlyMine = false)
        {
            var query = _context.OfferedRides
                .Include(r => r.Vehicle)
                .Include(r => r.Driver).ThenInclude(d => d.User)
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

            query = query.Where(r =>
                r.Status == RideStatus.Completed
                || r.Status == RideStatus.Cancelled
                || r.DepartureTime < DateTime.Now);

            if (onlyMine)
            {
                var uid = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(uid))
                {
                    return Forbid();
                }

                query = query.Where(r => r.DriverId.ToString() == uid);
                ViewBag.IsMyRidesMode = true;
            }
            else
            {
                ViewBag.IsMyRidesMode = false;
            }

            var rides = await query
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewData["CurrentFilterFrom"] = searchFrom;
            ViewData["CurrentFilterTo"] = searchTo;
            ViewData["CurrentFilterDate"] = searchDate?.ToString("yyyy-MM-dd");

            return View(rides);
        }

        // Moje przejazdy
        [Authorize]
        public async Task<IActionResult> MyRides()
        {
            // Przekierowujemy do Index z parametrem onlyMine, aby skorzystać z tego samego mechanizmu filtrowania
            return RedirectToAction("Index", new { onlyMine = true });
        }

        // Moje ukończone przejazdy
        [Authorize]
        public async Task<IActionResult> Completed()
        {
            var userId = _userManager.GetUserId(User);

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId.ToString() == userId);
            if (driverProfile == null)
            {
                return Forbid();
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
                .Include(r => r.Driver).ThenInclude(d => d.User)
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Where(b => b.RideId == id)
                .ToListAsync();

            ViewBag.Bookings = bookings;

            var passengerUserIds = bookings.Select(b => b.PassengerUserId).Distinct().ToList();
            var passengerProfiles = await _context.PassengerProfiles
                .Where(p => passengerUserIds.Contains(p.UserId))
                .ToListAsync();

            var ratingsDict = passengerProfiles.ToDictionary(p => p.UserId, p => p.Rating);
            ViewBag.PassengerRatings = ratingsDict;

            return View(ride);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var isDriver = User.IsInRole("Driver");

            if (!isAdmin && !isDriver)
            {
                // Tutaj ustawiamy komunikat błędu, który wyświetli się na stronie listy przejazdów
                TempData["Error"] = "Tylko zarejestrowani kierowcy mogą dodawać nowe trasy. Zostań kierowcą w swoim profilu!";
                return RedirectToAction("Index");
            }

            List<Vehicle> vehicles;

            if (isAdmin)
            {
                // Admin: wszystkie pojazdy
                vehicles = await _context.Vehicles.Include(v => v.Owner).ToListAsync(); // FIX: Usunięto .ThenInclude
            }
            else
            {
                vehicles = await _context.Vehicles
                    .Where(v => v.OwnerId.ToString() == userId)
                    .ToListAsync();
            }

            if (!vehicles.Any())
            {
                TempData["Error"] = "Nie masz dostępnych pojazdów. Dodaj pojazd, aby utworzyć przejazd.";
                return RedirectToAction("Index", "Vehicle"); // Przekierowanie do listy pojazdów
            }

            var model = new AddRideViewModel
            {
                AvailableVehicles = vehicles,
                DepartureTime = DateTime.Now.AddHours(1)
            };
            return View(model);
        }

        // --- Create (POST) - POPRAWIONE ---
        // --- Create (POST) - POPRAWIONE ---
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

            // --- DODANA WALIDACJA DAT ---
            if (model.DepartureTime < DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "Data wyjazdu nie może być w przeszłości.");
            }

            if (model.ArrivalTime.HasValue && model.ArrivalTime.Value <= model.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Data przyjazdu musi być późniejsza niż data wyjazdu.");
            }
            // ----------------------------

            if (!ModelState.IsValid)
            {
                var uid = _userManager.GetUserId(User);
                if (User.IsInRole("Admin"))
                    model.AvailableVehicles = await _context.Vehicles.Include(v => v.Owner).ToListAsync();
                else
                    model.AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == uid).ToListAsync();

                return View(model);
            }

            // Tutaj .Include(v => v.Owner) jest OK, bo nie ma ThenInclude
            var vehicle = await _context.Vehicles.Include(v => v.Owner).FirstOrDefaultAsync(v => v.Id == model.SelectedVehicleId);
            if (vehicle == null) return NotFound();

            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && vehicle.OwnerId != currentUserId)
            {
                return Forbid();
            }

            Guid driverId = vehicle.OwnerId;

            var driverProfile = await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == driverId);
            if (driverProfile == null)
            {
                ModelState.AddModelError("", "Właściciel wybranego pojazdu nie posiada aktywnego profilu kierowcy.");

                var uid = _userManager.GetUserId(User);
                if (isAdmin)
                    model.AvailableVehicles = await _context.Vehicles.Include(v => v.Owner).ToListAsync();
                else
                    model.AvailableVehicles = await _context.Vehicles.Where(v => v.OwnerId.ToString() == uid).ToListAsync();
                return View(model);
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

            _context.OfferedRides.Add(ride);
            await _context.SaveChangesAsync();

            // Usuń powiadomienie o zaakceptowaniu wniosku o zostanie kierowcą po dodaniu pierwszego przejazdu
            try
            {
                var acceptNotifications = await _context.Notifications
                    .Where(n => n.UserId == driverId && n.Title == "Wniosek zaakceptowany")
                    .ToListAsync();
                if (acceptNotifications.Any())
                {
                    _context.Notifications.RemoveRange(acceptNotifications);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // ignoruj błędy powiadomień
            }

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
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

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

            ViewBag.RideId = ride.Id;
            return View(model);
        }

        // Edit (POST)
        // Edit (POST)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AddRideViewModel model)
        {
            ModelState.Remove("AvailableVehicles");
            var userId = _userManager.GetUserId(User);

            // --- DODANA WALIDACJA DAT ---
            if (model.DepartureTime < DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "Data wyjazdu nie może być w przeszłości.");
            }

            if (model.ArrivalTime.HasValue && model.ArrivalTime.Value <= model.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Data przyjazdu musi być późniejsza niż data wyjazdu.");
            }
            // ----------------------------

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

            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

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

        // Summary (GET)
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

        // POST: RatePassenger
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RatePassenger(Guid rideId, Guid passengerUserId, int rating, string? comment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);
            if (ride == null) return NotFound();

            if (!User.IsInRole("Admin") && ride.DriverId != currentUser.Id)
                return Forbid();

            if (ride.Status != RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Oceny można wystawić dopiero po zakończeniu przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.RideId == rideId && b.PassengerUserId == passengerUserId);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Ten użytkownik nie był pasażerem tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

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

        // Complete (POST)
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
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

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
            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

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

        // --- AWARYJNE ODWOŁANIE PRZEJAZDU PRZEZ KIEROWCĘ ---
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRide(Guid id)
        {
            var ride = await _context.OfferedRides
                .Include(r => r.Bookings)
                .Include(r => r.StartLocation) 
                .Include(r => r.EndLocation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (!User.IsInRole("Admin") && ride.DriverId.ToString() != userId)
            {
                return Forbid();
            }

            if (ride.Status == RideStatus.Cancelled || ride.Status == RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Tego przejazdu nie można już odwołać.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ride.Status = RideStatus.Cancelled;

            foreach (var booking in ride.Bookings)
            {
                if (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
                {
                    booking.Status = BookingStatus.Cancelled;
                    booking.CommentByDriver = "Przejazd został awaryjnie odwołany przez kierowcę.";

                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = booking.PassengerUserId,
                        Title = "Ważne: Przejazd odwołany!",
                        Body = $"Kierowca awaryjnie odwołał przejazd na trasie {ride.StartLocation?.City} - {ride.EndLocation?.City} z dnia {ride.DepartureTime:dd.MM.yyyy HH:mm}.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    _context.Notifications.Add(notification);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Przejazd został awaryjnie odwołany. Zapisani pasażerowie otrzymali odpowiednie powiadomienie.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: RateDriver
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateDriver(Guid rideId, int rating, string? comment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);
            if (ride == null) return NotFound();

            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.RideId == rideId && b.PassengerUserId == currentUser.Id);
            if (booking == null && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Nie możesz ocenić tego kierowcy — nie byłeś pasażerem tego przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            if (ride.Status != RideStatus.Completed)
            {
                TempData["ErrorMessage"] = "Oceny można wystawić dopiero po zakończeniu przejazdu.";
                return RedirectToAction("Summary", new { id = rideId });
            }

            var exists = await _context.Reviews.FirstOrDefaultAsync(r =>
                r.RideId == rideId && r.FromUserId == currentUser.Id && r.ToUserId == ride.DriverId);

            if (exists != null)
            {
                TempData["ErrorMessage"] = "Już oceniłeś tego kierowcy dla tego przejazdu.";
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
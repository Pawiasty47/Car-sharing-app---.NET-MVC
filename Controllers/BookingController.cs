using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    [Authorize] // Tylko zalogowani użytkownicy mogą rezerwować
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public BookingController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. MOJE REZERWACJE (Lista biletów pasażera)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myBookings = await _context.Bookings
                // 1. Pobierz Przejazd
                .Include(b => b.Ride)
                    // 2. Pobierz Lokalizacje Start/Koniec z tego przejazdu
                    .ThenInclude(r => r.StartLocation)
                .Include(b => b.Ride)
                    .ThenInclude(r => r.EndLocation)
                // 3. Pobierz Kierowcę i jego dane osobowe (User)
                .Include(b => b.Ride)
                    .ThenInclude(r => r.Driver)
                        .ThenInclude(d => d.User)
                // Filtrowanie i sortowanie
                .Where(b => b.PassengerUserId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(myBookings);
        }

        // 2. REZERWACJA MIEJSCA (Akcja POST z widoku Details)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid rideId, int seats = 1, string? comment = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Pobieramy przejazd z bazy
            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);
            if (ride == null) return NotFound();

            // --- WALIDACJE ---

            // A. Czy kierowca próbuje zarezerwować u siebie?
            if (ride.DriverId == user.Id)
            {
                TempData["ErrorMessage"] = "Nie możesz zarezerwować miejsca we własnym przejeździe.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // B. Czy jest wystarczająco miejsc?
            if ((ride.SeatsOffered - ride.SeatsTaken) < seats)
            {
                TempData["ErrorMessage"] = "Niestety, brak wystarczającej liczby wolnych miejsc.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // C. Czy użytkownik już ma rezerwację na ten przejazd?
            bool alreadyBooked = await _context.Bookings
                .AnyAsync(b => b.RideId == rideId && b.PassengerUserId == user.Id);

            if (alreadyBooked)
            {
                TempData["ErrorMessage"] = "Masz już aktywną rezerwację na ten przejazd.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // --- ZAPIS ---
            var booking = new Booking
            {
                RideId = rideId,
                PassengerUserId = user.Id,
                SeatsRequested = seats,
                CommentByPassenger = comment,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            // Zwiększamy licznik zajętych miejsc w przejeździe
            ride.SeatsTaken += seats;

            _context.Bookings.Add(booking);
            // EF Core zaktualizuje też ride.SeatsTaken, bo obiekt jest śledzony
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rezerwacja udana! Znajdziesz ją w zakładce 'Moje bilety'.";
            return RedirectToAction("Details", "Rides", new { id = rideId });
        }

        // 3. ANULOWANIE REZERWACJI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // Sprawdzamy, czy to rezerwacja tego użytkownika
            if (booking.PassengerUserId != user.Id)
            {
                return Forbid();
            }

            // Przywracamy wolne miejsca w przejeździe
            if (booking.Ride != null)
            {
                booking.Ride.SeatsTaken -= booking.SeatsRequested;
                if (booking.Ride.SeatsTaken < 0) booking.Ride.SeatsTaken = 0; // Zabezpieczenie
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rezerwacja została anulowana.";
            return RedirectToAction(nameof(Index));
        }
    }
}
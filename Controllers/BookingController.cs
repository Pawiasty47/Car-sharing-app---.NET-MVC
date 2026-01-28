using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
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
                .Include(b => b.Ride).ThenInclude(r => r.StartLocation)
                .Include(b => b.Ride).ThenInclude(r => r.EndLocation)
                .Include(b => b.Ride).ThenInclude(r => r.Driver).ThenInclude(d => d.User)
                .Where(b => b.PassengerUserId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(myBookings);
        }

        // 2. TWORZENIE REZERWACJI (Domyślnie status PENDING)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid rideId, int seats = 1, string? comment = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);

            if (ride == null) return NotFound();

            // Walidacja: Kierowca nie może rezerwować u siebie
            if (ride.DriverId == user.Id)
            {
                TempData["ErrorMessage"] = "Nie możesz zarezerwować miejsca we własnym przejeździe.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // Walidacja: Dostępność miejsc (wstępna)
            if ((ride.SeatsOffered - ride.SeatsTaken) < seats)
            {
                TempData["ErrorMessage"] = "Brak wystarczającej liczby wolnych miejsc.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // Walidacja: Duplikaty (aktywne lub oczekujące)
            bool alreadyBooked = await _context.Bookings
                .AnyAsync(b => b.RideId == rideId && b.PassengerUserId == user.Id &&
                               (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));

            if (alreadyBooked)
            {
                TempData["ErrorMessage"] = "Masz już aktywną rezerwację lub oczekującą prośbę na ten przejazd.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            var booking = new Booking
            {
                RideId = rideId,
                PassengerUserId = user.Id,
                SeatsRequested = seats,
                CommentByPassenger = comment,
                Status = BookingStatus.Pending, // Oczekuje na akceptację
                CreatedAt = DateTime.UtcNow
            };

            // WAŻNE: Nie zwiększamy ride.SeatsTaken tutaj. Robimy to dopiero przy akceptacji.

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Wysłano prośbę o rezerwację! Czekaj na potwierdzenie kierowcy.";
            return RedirectToAction("Details", "Rides", new { id = rideId });
        }

        // 3. AKCEPTACJA PASAŻERA (Dla Kierowcy i Admina)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // POPRAWKA: Dodano obsługę Admina
            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Sprawdź czy NADAL są miejsca (mogły zniknąć w międzyczasie)
            if ((booking.Ride.SeatsOffered - booking.Ride.SeatsTaken) < booking.SeatsRequested)
            {
                TempData["ErrorMessage"] = "Nie możesz zaakceptować - brak wolnych miejsc w pojeździe!";
                return RedirectToAction("Details", "Rides", new { id = booking.RideId });
            }

            // Zmiana statusu i zajęcie miejsc
            booking.Status = BookingStatus.Confirmed;
            booking.Ride.SeatsTaken += booking.SeatsRequested;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zaakceptowano pasażera. Miejsca zostały zaktualizowane.";
            return RedirectToAction("Details", "Rides", new { id = booking.RideId });
        }

        // 4. ODRZUCENIE PASAŻERA Z POWODEM (Dla Kierowcy i Admina)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid bookingId, string? reason) // <--- ZMIANA: Dodano parametr reason
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            // Sprawdzenie uprawnień: Właściciel lub Admin
            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            booking.Status = BookingStatus.Rejected;
            booking.CommentByDriver = reason; // <--- ZMIANA: Zapisujemy powód w bazie

            // Nie zwalniamy miejsc, bo przy statusie Pending (oczekująca) nie były one zajęte

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odrzucono prośbę o rezerwację.";
            return RedirectToAction("Details", "Rides", new { id = booking.RideId });
        }

        // 5. ANULOWANIE / USUWANIE REZERWACJI (Przez pasażera)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // Tylko właściciel rezerwacji może ją anulować
            if (booking.PassengerUserId != user.Id) return Forbid();

            // POPRAWKA LOGIKI:
            // Scenariusz A: Rezerwacja była ZATWIERDZONA -> Zwalniamy miejsce w aucie i oznaczamy jako Anulowana
            if (booking.Status == BookingStatus.Confirmed && booking.Ride != null)
            {
                booking.Ride.SeatsTaken -= booking.SeatsRequested;
                if (booking.Ride.SeatsTaken < 0) booking.Ride.SeatsTaken = 0;

                booking.Status = BookingStatus.Cancelled;
                TempData["SuccessMessage"] = "Rezerwacja została anulowana. Miejsca zwolnione.";
            }
            // Scenariusz B: Rezerwacja była PENDING, REJECTED lub już CANCELLED -> Usuwamy fizycznie (sprzątanie listy)
            else
            {
                _context.Bookings.Remove(booking);
                TempData["SuccessMessage"] = "Rezerwacja została usunięta z listy.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
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

            // Walidacja: Dostępność miejsc (wstępna - zablokowanie overbookingu)
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

            // ZMIANA: Natychmiastowo zajmujemy miejsce, mimo że to "Pending". Zapobiega overbookingowi.
            ride.SeatsTaken += seats;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Wysłano prośbę o rezerwację! Miejsce zostało dla Ciebie zablokowane.";
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

            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            booking.Status = BookingStatus.Confirmed;

            // ✅ TWORZENIE / POBIERANIE CHATA
            var chat = await _context.Chats
                .FirstOrDefaultAsync(c => c.RideId == booking.RideId);

            if (chat == null)
            {
                chat = new Chat
                {
                    Id = Guid.NewGuid(),
                    RideId = booking.RideId
                };

                _context.Chats.Add(chat);

                // kierowca
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    Id = Guid.NewGuid(),
                    ChatId = chat.Id,
                    UserId = booking.Ride.DriverId
                });
            }

            // pasażer
            bool alreadyParticipant = await _context.ChatParticipants
                .AnyAsync(p => p.ChatId == chat.Id && p.UserId == booking.PassengerUserId);

            if (!alreadyParticipant)
            {
                _context.ChatParticipants.Add(new ChatParticipant
                {
                    Id = Guid.NewGuid(),
                    ChatId = chat.Id,
                    UserId = booking.PassengerUserId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zaakceptowano pasażera.";
            return RedirectToAction("Details", "Rides", new { id = booking.RideId });
        }

        // 4. ODRZUCENIE PASAŻERA Z POWODEM (Dla Kierowcy i Admina)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid bookingId, string? reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // ZMIANA: Skoro przy "Pending" zajęliśmy miejsca, to przy odrzuceniu musimy je oddać!
            if (booking.Status == BookingStatus.Pending && booking.Ride != null)
            {
                booking.Ride.SeatsTaken -= booking.SeatsRequested;
                if (booking.Ride.SeatsTaken < 0) booking.Ride.SeatsTaken = 0; // Zabezpieczenie na wszelki wypadek
            }

            booking.Status = BookingStatus.Rejected;
            booking.CommentByDriver = reason;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Odrzucono prośbę o rezerwację. Miejsca wróciły do puli.";
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

            if (booking.PassengerUserId != user.Id) return Forbid();

            // ZMIANA LOGIKI ZWALNIANIA MIEJSC:
            // Musimy oddać miejsca, jeśli pasażer zrezygnuje zarówno jako ZATWIERDZONY, jak i w trakcie OCZEKIWANIA
            if ((booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Pending) && booking.Ride != null)
            {
                booking.Ride.SeatsTaken -= booking.SeatsRequested;
                if (booking.Ride.SeatsTaken < 0) booking.Ride.SeatsTaken = 0;
            }

            if (booking.Status == BookingStatus.Confirmed)
            {
                booking.Status = BookingStatus.Cancelled;
                TempData["SuccessMessage"] = "Rezerwacja została anulowana. Zarezerwowane miejsca zostały zwolnione.";
            }
            else
            {
                // Jeśli był Pending, Rejected lub już Cancelled -> po prostu usuwamy z listy,
                // a jeśli był Pending, to miejsca zostały oddane parę linijek wyżej.
                _context.Bookings.Remove(booking);
                TempData["SuccessMessage"] = "Zgłoszenie zostało usunięte z Twojej listy.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
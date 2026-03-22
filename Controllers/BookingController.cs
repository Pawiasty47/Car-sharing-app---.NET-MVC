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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid rideId, int seats = 1, string? comment = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var ride = await _context.OfferedRides.FirstOrDefaultAsync(r => r.Id == rideId);

            if (user == null || ride == null) return NotFound();

            // 1. Walidacje podstawowe
            if (ride.DriverId == user.Id)
            {
                TempData["ErrorMessage"] = "Nie możesz zarezerwować miejsca we własnym przejeździe.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            if ((ride.SeatsOffered - ride.SeatsTaken) < seats)
            {
                TempData["ErrorMessage"] = "Brak wystarczającej liczby wolnych miejsc.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // 2. Walidacja SALDA (Mrożenie środków)
            decimal totalToFreeze = ride.PricePerSeat * seats;
            if (user.Balance < totalToFreeze)
            {
                TempData["ErrorMessage"] = $"Niewystarczające środki na koncie. Potrzebujesz {totalToFreeze:C2}.";
                return RedirectToAction("Details", "Rides", new { id = rideId });
            }

            // 3. TRANSAKCJA - mrozimy kasę i tworzymy rezerwację
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Odejmij środki z portfela pasażera
                    user.Balance -= totalToFreeze;
                    await _userManager.UpdateAsync(user);

                    var booking = new Booking
                    {
                        RideId = rideId,
                        PassengerUserId = user.Id,
                        SeatsRequested = seats,
                        CommentByPassenger = comment,
                        Status = BookingStatus.Pending,
                        FrozenAmount = totalToFreeze, // Zapisujemy ile zamroziliśmy
                        CreatedAt = DateTime.UtcNow
                    };

                    ride.SeatsTaken += seats;
                    _context.Bookings.Add(booking);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Wysłano prośbę! Zablokowano {totalToFreeze:C2} na Twoim koncie.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Wystąpił błąd podczas przetwarzania rezerwacji. Spróbuj ponownie.";
                }
            }

            return RedirectToAction("Details", "Rides", new { id = rideId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);

            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin")) return Forbid();

            booking.Status = BookingStatus.Confirmed;

            // Logika czatu (bez zmian w stosunku do Twojego kodu)
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.RideId == booking.RideId);
            if (chat == null)
            {
                chat = new Chat { Id = Guid.NewGuid(), RideId = booking.RideId };
                _context.Chats.Add(chat);
                _context.ChatParticipants.Add(new ChatParticipant { Id = Guid.NewGuid(), ChatId = chat.Id, UserId = booking.Ride.DriverId });
            }

            if (!await _context.ChatParticipants.AnyAsync(p => p.ChatId == chat.Id && p.UserId == booking.PassengerUserId))
            {
                _context.ChatParticipants.Add(new ChatParticipant { Id = Guid.NewGuid(), ChatId = chat.Id, UserId = booking.PassengerUserId });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Zaakceptowano pasażera. Środki pozostają zablokowane do czasu zakończenia trasy.";
            return RedirectToAction("Details", "Rides", new { id = booking.RideId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid bookingId, string? reason)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .Include(b => b.Passenger)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);

            if (booking.Ride.DriverId != user.Id && !User.IsInRole("Admin")) return Forbid();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // ZWROT ŚRODKÓW przy odrzuceniu
                    if (booking.FrozenAmount > 0)
                    {
                        booking.Passenger.Balance += booking.FrozenAmount;
                        await _userManager.UpdateAsync(booking.Passenger);
                    }

                    if (booking.Status == BookingStatus.Pending)
                    {
                        booking.Ride.SeatsTaken -= booking.SeatsRequested;
                    }

                    booking.Status = BookingStatus.Rejected;
                    booking.CommentByDriver = reason;
                    decimal returnedAmount = booking.FrozenAmount;
                    booking.FrozenAmount = 0; // Wyczyszczenie po zwrocie

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Odrzucono prośbę. Środki ({returnedAmount:C2}) wróciły do pasażera.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Błąd podczas odrzucania rezerwacji.";
                }
            }

            return RedirectToAction("Details", "Rides", new { id = booking.RideId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .Include(b => b.Passenger)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            if (booking.PassengerUserId != user.Id) return Forbid();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // ZWROT ŚRODKÓW przy anulowaniu przez pasażera
                    if (booking.FrozenAmount > 0)
                    {
                        user.Balance += booking.FrozenAmount;
                        await _userManager.UpdateAsync(user);
                    }

                    if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Pending)
                    {
                        booking.Ride.SeatsTaken -= booking.SeatsRequested;
                    }

                    decimal returnedAmount = booking.FrozenAmount;
                    booking.FrozenAmount = 0;

                    if (booking.Status == BookingStatus.Confirmed)
                    {
                        booking.Status = BookingStatus.Cancelled;
                    }
                    else
                    {
                        _context.Bookings.Remove(booking);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Anulowano. Zwrócono {returnedAmount:C2} na Twoje konto.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Błąd podczas anulowania rezerwacji.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Services
{
    public class RideNotificationService
    {
        private readonly AppDbContext _context;

        public RideNotificationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Sprawdza subskrypcje i wysyła powiadomienia dla pasujących przejazdów
        /// </summary>
        public async Task CheckAndNotifyAsync(OfferedRide ride)
        {
            if (ride == null) return;

            // Pobierz aktywne subskrypcje pasujące do trasy i daty
            var subscriptions = await _context.RideSubscriptions
                .Where(s => s.IsActive &&
                            s.RideDate.Date == ride.DepartureTime.Date &&
                            s.FromCity.ToLower() == ride.StartLocation.City.ToLower() &&
                            s.ToCity.ToLower() == ride.EndLocation.City.ToLower())
                .Include(s => s.User)
                .ToListAsync();

            foreach (var sub in subscriptions)
            {
                // Dodaj powiadomienie w bazie danych
                var notification = new Notification
                {
                    UserId = sub.UserId,
                    Title = "Nowa trasa pasująca do Twojej subskrypcji",
                    Body = $"Pojawił się nowy przejazd: {ride.StartLocation.City} → {ride.EndLocation.City} w dniu {ride.DepartureTime:dd.MM.yyyy HH:mm}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
    }
}
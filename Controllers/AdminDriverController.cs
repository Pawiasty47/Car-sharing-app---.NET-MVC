using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    // [Authorize(Roles = "Admin")] // Odkomentuj, gdy będziesz mieć gotowe role w systemie!
    public class AdminDriverController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public AdminDriverController(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Lista wszystkich oczekujących wniosków
        public async Task<IActionResult> Index()
        {
            var apps = await _context.DriverApplications
                .Include(d => d.User)
                .Include(d => d.Vehicle)
                .Where(d => d.Status == ApplicationStatus.Pending)
                .ToListAsync();
            return View(apps);
        }

        // Akcja: Akceptuj
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var app = await _context.DriverApplications.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (app == null) return NotFound();

            // 1. Zmień status wniosku
            app.Status = ApplicationStatus.Approved;
            app.AdminFeedback = "Wniosek zaakceptowany poprawnie.";

            // 2. Nadaj rolę "Driver" użytkownikowi
            // Najpierw sprawdzamy, czy rola istnieje, jak nie to tworzymy
            if (!await _roleManager.RoleExistsAsync("Driver"))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("Driver"));
            }

            await _userManager.AddToRoleAsync(app.User, "Driver");

            // Dodaj powiadomienie dla autora wniosku
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = app.UserId,
                    Title = "Wniosek zaakceptowany",
                    Body = "Twój wniosek o status kierowcy został zaakceptowany. Możesz teraz dodawać przejazdy.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }
            catch
            {
                // ignoruj błędy powiadomień
            }

            // Utwórz profil kierowcy, jeśli jeszcze nie istnieje (po zatwierdzeniu wniosku)
            try
            {
                var existingProfile = await _context.DriverProfiles.FindAsync(app.UserId);
                if (existingProfile == null)
                {
                    var profile = new DriverProfile
                    {
                        UserId = app.UserId,
                        User = app.User,
                        IsVerified = true,
                        CompletedRidesCount = 0,
                        Rating = 0.0
                    };
                    _context.DriverProfiles.Add(profile);
                }
            }
            catch
            {
                // ignoruj błędy tworzenia profilu, nie blokujemy akceptacji
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Akcja: Odrzuć
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var app = await _context.DriverApplications.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (app == null) return NotFound();

            app.Status = ApplicationStatus.Rejected;
            app.AdminFeedback = string.IsNullOrEmpty(reason) ? "Brak podanego powodu" : reason;

            // Powiadom autora wniosku o odrzuceniu
            try
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = app.UserId,
                    Title = "Wniosek odrzucony",
                    Body = $"Twój wniosek o status kierowcy został odrzucony. Powód: {app.AdminFeedback}",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
            }
            catch
            {
                // ignoruj błędy powiadomień
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
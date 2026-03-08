using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using projekt_zespołowy.Models.ViewModels;

namespace projekt_zespołowy.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // --- DLA ADMINISTRATORA: Lista zgłoszeń ---
        public async Task<IActionResult> Index()
        {
            // Tutaj też warto by dodać zabezpieczenie, np.:
            // if (!User.IsInRole("Admin")) return Forbid(); 

            var reports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Pobierz oczekujące wnioski o status kierowcy, aby admin mógł je akceptować w jednym panelu
            var driverApps = await _context.DriverApplications
                .Include(d => d.User)
                .Include(d => d.Vehicle)
                .Where(d => d.Status == ApplicationStatus.Pending)
                .OrderByDescending(d => d.ApplicationDate)
                .ToListAsync();

            var model = new projekt_zespołowy.Models.ViewModels.AdminReportsViewModel
            {
                Reports = reports,
                DriverApplications = driverApps
            };

            return View(model);
        }

        // --- DLA UŻYTKOWNIKA: Formularz ---
        [HttpGet]
        public IActionResult Create()
        {
            // ZABEZPIECZENIE: Tylko dla zalogowanych
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                // Jeśli nie jest zalogowany, przekieruj do logowania
                // (Upewnij się, że masz AccountController i akcję Login)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Report/Create" });
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportViewModel model)
        {
            // Ponowne sprawdzenie przy wysyłaniu
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Próba znalezienia usera po nazwie
            Guid? currentUserId = null;
            if (User.Identity.Name != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                if (user != null)
                {
                    currentUserId = user.Id;
                }
            }

            var report = new AppReport
            {
                Id = Guid.NewGuid(),
                Category = model.Category,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow,
                UserId = currentUserId,
                IsResolved = false
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Zgłoszenie zostało wysłane!";
            return RedirectToAction("Index", "Home");
        }

        // --- ADMIN: Oznacz jako rozwiązane ---
        [HttpPost]
        public async Task<IActionResult> MarkAsResolved(Guid id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.IsResolved = !report.IsResolved;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- NOWOŚĆ: ADMIN - USUWANIE ZGŁOSZENIA ---
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Zgłoszenie zostało usunięte.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

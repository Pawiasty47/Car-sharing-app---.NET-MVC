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

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Akcja: Odrzuć
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var app = await _context.DriverApplications.FindAsync(id);
            if (app == null) return NotFound();

            app.Status = ApplicationStatus.Rejected;
            app.AdminFeedback = string.IsNullOrEmpty(reason) ? "Brak podanego powodu" : reason;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
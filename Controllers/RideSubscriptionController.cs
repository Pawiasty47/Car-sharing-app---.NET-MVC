using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    [Authorize]
    public class RideSubscriptionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public RideSubscriptionController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var subscriptions = await _context.RideSubscriptions
                .Where(s => s.UserId == user.Id && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(subscriptions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string fromCity, string toCity, DateTime rideDate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(fromCity) || string.IsNullOrWhiteSpace(toCity))
            {
                TempData["ErrorMessage"] = "Musisz podać miasto początkowe i docelowe.";
                return RedirectToAction(nameof(Index));
            }

            if (rideDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Nie możesz ustawić subskrypcji na datę z przeszłości.";
                return RedirectToAction(nameof(Index));
            }

            var subscription = new RideSubscription
            {
                UserId = user.Id,
                FromCity = fromCity.Trim(),
                ToCity = toCity.Trim(),
                RideDate = rideDate.Date,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.RideSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subskrypcja została zapisana.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var subscription = await _context.RideSubscriptions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == user.Id);

            if (subscription == null)
            {
                TempData["ErrorMessage"] = "Nie znaleziono subskrypcji.";
                return RedirectToAction(nameof(Index));
            }

            _context.RideSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Subskrypcja została usunięta.";
            return RedirectToAction(nameof(Index));
        }
    }
}
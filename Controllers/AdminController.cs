using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projekt_zespołowy.Controllers
{
    [Authorize(Roles = "Admin")] // Zabezpieczenie: Tylko dla Admina
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _db;

        public AdminController(UserManager<User> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            // 1. Pobieramy wszystkich użytkowników
            var users = await _db.Users.ToListAsync();

            // 2. Pobieramy ID użytkowników, którzy mają profil w DriverProfiles
            var profileUserIds = await _db.DriverProfiles
                .Select(d => d.UserId)
                .ToListAsync();

            // 3. Pobieramy ID użytkowników, którzy mają rolę "Driver" (z Identity)
            // To jest kluczowe, bo Anna może mieć rolę, ale brak profilu (np. błąd przy tworzeniu)
            var usersInDriverRole = await _userManager.GetUsersInRoleAsync("Driver");
            var roleUserIds = usersInDriverRole.Select(u => u.Id).ToList();

            var model = new List<AdminUserListVM>();

            foreach (var user in users)
            {
                // Sprawdzamy czy ID jest w liście profili LUB w liście ról
                bool hasProfile = profileUserIds.Contains(user.Id);
                bool hasRole = roleUserIds.Contains(user.Id);

                var vm = new AdminUserListVM
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}",

                    // Uznajemy za kierowcę, jeśli ma profil ALBO rolę (dla pewności)
                    IsDriver = hasProfile || hasRole
                };

                model.Add(vm);
            }

            return View(model);
        }

        // POST: /Admin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return RedirectToAction(nameof(Users));
            }

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id.ToString() == currentUserId)
            {
                TempData["Error"] = "Nie możesz usunąć konta, na którym jesteś zalogowany!";
                return RedirectToAction(nameof(Users));
            }

            // Najpierw usuwamy zależne dane (jeśli kaskadowanie w bazie nie zadziała)
            var driverProfile = await _db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (driverProfile != null) _db.DriverProfiles.Remove(driverProfile);

            var passengerProfile = await _db.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (passengerProfile != null) _db.PassengerProfiles.Remove(passengerProfile);

            await _db.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Użytkownik został usunięty.";
            }
            else
            {
                TempData["Error"] = "Wystąpił błąd podczas usuwania użytkownika.";
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
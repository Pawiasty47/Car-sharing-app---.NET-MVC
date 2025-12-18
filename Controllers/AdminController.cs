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
        // Wyświetla listę użytkowników
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            var driverIds = await _db.DriverProfiles.Select(d => d.UserId).ToListAsync();

            var model = new List<AdminUserListVM>();

            foreach (var user in users)
            {
                var vm = new AdminUserListVM
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}",
                    IsDriver = driverIds.Contains(user.Id)
                };

                model.Add(vm);
            }

            return View(model);
        }

        // POST: /Admin/Delete/5
        // Usuwa użytkownika z bazy
        [HttpPost]
        [ValidateAntiForgeryToken] // Zabezpieczenie przed atakami CSRF
        public async Task<IActionResult> Delete(string id)
        {
            // 1. Znajdź użytkownika do usunięcia
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                // Jeśli nie ma takiego usera, wyświetl błąd lub wróć do listy
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return RedirectToAction(nameof(Users));
            }

            // 2. ZABEZPIECZENIE: Nie pozwól usunąć samego siebie!
            // Pobieramy ID aktualnie zalogowanego admina
            var currentUserId = _userManager.GetUserId(User);

            if (user.Id.ToString() == currentUserId)
            {
                TempData["Error"] = "Nie możesz usunąć konta, na którym jesteś zalogowany!";
                return RedirectToAction(nameof(Users));
            }

            // 3. Usuwanie
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
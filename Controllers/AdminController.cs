using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using projekt_zespołowy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projekt_zespołowy.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _db;

        public AdminController(UserManager<User> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardVM();

            vm.TotalDrivers = await _db.DriverProfiles.CountAsync();
            int totalUsers = await _db.Users.CountAsync();
            vm.TotalPassengers = totalUsers - vm.TotalDrivers;

            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var recentRides = await _db.OfferedRides
                .Where(r => r.DepartureTime >= sevenDaysAgo)
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                vm.Last7DaysLabels.Add(date.ToString("dd.MM"));
                vm.Last7DaysData.Add(recentRides.Count(r => r.DepartureTime.Date == date));
            }

            vm.TopCities = await _db.OfferedRides
                .Include(r => r.EndLocation)
                .Where(r => r.EndLocation != null && !string.IsNullOrEmpty(r.EndLocation.City))
                .GroupBy(r => r.EndLocation.City)
                .Select(g => new CityStat { CityName = g.Key, RideCount = g.Count() })
                .OrderByDescending(c => c.RideCount)
                .Take(5)
                .ToListAsync();

            var oneMonthAgo = DateTime.Today.AddMonths(-1);
            var earnings = await _db.Bookings
                .Where(b => b.Ride.DepartureTime >= oneMonthAgo && b.Status == BookingStatus.Confirmed)
                .GroupBy(b => new { b.Ride.Driver.User.FirstName, b.Ride.Driver.User.LastName })
                .Select(g => new
                {
                    DriverName = g.Key.FirstName + " " + g.Key.LastName,
                    Earnings = g.Sum(b => b.Ride.PricePerSeat)
                })
                .OrderByDescending(x => x.Earnings)
                .Take(5)
                .ToListAsync();

            foreach (var e in earnings)
            {
                vm.DriverEarningsLabels.Add(e.DriverName);
                vm.DriverEarningsData.Add((decimal)e.Earnings);
            }

            vm.PopularRoutes = await _db.OfferedRides
                .Include(r => r.StartLocation)
                .Include(r => r.EndLocation)
                .Where(r => r.StartLocation != null && r.EndLocation != null)
                .GroupBy(r => r.StartLocation.City + " - " + r.EndLocation.City)
                .Select(g => new RouteStat { RouteName = g.Key, RideCount = g.Count() })
                .OrderByDescending(r => r.RideCount)
                .Take(5)
                .ToListAsync();

            vm.TopPassengers = await _db.Bookings
                .Include(b => b.Passenger)
                .Where(b => b.Status == BookingStatus.Confirmed)
                .GroupBy(b => new { b.Passenger.FirstName, b.Passenger.LastName })
                .Select(g => new PassengerStat
                {
                    FullName = g.Key.FirstName + " " + g.Key.LastName,
                    RideCount = g.Count()
                })
                .OrderByDescending(p => p.RideCount)
                .Take(5)
                .ToListAsync();

            var allDates = await _db.OfferedRides.Select(r => r.DepartureTime).ToListAsync();
            var daysGroup = allDates.GroupBy(d => d.DayOfWeek).ToDictionary(g => g.Key, g => g.Count());

            var polishDays = new Dictionary<DayOfWeek, string> {
                { DayOfWeek.Monday, "Pn" }, { DayOfWeek.Tuesday, "Wt" },
                { DayOfWeek.Wednesday, "Śr" }, { DayOfWeek.Thursday, "Czw" },
                { DayOfWeek.Friday, "Pt" }, { DayOfWeek.Saturday, "Sb" },
                { DayOfWeek.Sunday, "Nd" }
            };

            var orderedDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            foreach (var day in orderedDays)
            {
                vm.DaysOfWeekLabels.Add(polishDays[day]);
                vm.DaysOfWeekData.Add(daysGroup.ContainsKey(day) ? daysGroup[day] : 0);
            }

            vm.AllDriversEarnings = await _db.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed)
                .GroupBy(b => new { b.Ride.Driver.User.FirstName, b.Ride.Driver.User.LastName })
                .Select(g => new DriverAnalysisStat
                {
                    DriverName = g.Key.FirstName + " " + g.Key.LastName,
                    CompletedRides = g.Count(),
                    TotalEarnings = g.Sum(b => b.Ride.PricePerSeat)
                })
                .OrderByDescending(d => d.TotalEarnings)
                .ToListAsync();

            return View(vm);
        }

        public async Task<IActionResult> Users(string? search)
        {
            var usersQuery = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.FirstName + " " + u.LastName).ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
            }

            var users = await usersQuery.ToListAsync();

            var profileUserIds = await _db.DriverProfiles.Select(d => d.UserId).ToListAsync();
            var usersInDriverRole = await _userManager.GetUsersInRoleAsync("Driver");
            var roleUserIds = usersInDriverRole.Select(u => u.Id).ToList();

            var model = new List<AdminUserListVM>();

            foreach (var user in users)
            {
                bool hasProfile = profileUserIds.Contains(user.Id);
                bool hasRole = roleUserIds.Contains(user.Id);

                model.Add(new AdminUserListVM
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = $"{user.FirstName} {user.LastName}",
                    IsDriver = hasProfile || hasRole
                });
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) { TempData["Error"] = "Nie znaleziono użytkownika."; return RedirectToAction(nameof(Users)); }

            if (user.Id.ToString() == _userManager.GetUserId(User))
            {
                TempData["Error"] = "Nie możesz usunąć konta, na którym jesteś zalogowany!";
                return RedirectToAction(nameof(Users));
            }

            var driverProfile = await _db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (driverProfile != null) _db.DriverProfiles.Remove(driverProfile);

            var passengerProfile = await _db.PassengerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (passengerProfile != null) _db.PassengerProfiles.Remove(passengerProfile);

            await _db.SaveChangesAsync();
            var result = await _userManager.DeleteAsync(user);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Użytkownik został usunięty." : "Wystąpił błąd.";
            return RedirectToAction(nameof(Users));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeDriver(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Driver"))
            {
                TempData["Error"] = $"Użytkownik {user.FirstName} ma już uprawnienia kierowcy.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.AddToRoleAsync(user, "Driver");

            if (result.Succeeded)
            {
                TempData["Success"] = $"Pomyślnie nadano uprawnienia kierowcy użytkownikowi {user.FirstName} {user.LastName}.";
            }
            else
            {
                TempData["Error"] = "Wystąpił błąd podczas nadawania uprawnień kierowcy.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return RedirectToAction(nameof(Users));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"Użytkownik {user.FirstName} jest już administratorem.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                TempData["Success"] = $"Pomyślnie nadano uprawnienia administratora użytkownikowi {user.FirstName} {user.LastName}.";
            }
            else
            {
                TempData["Error"] = "Wystąpił błąd podczas nadawania uprawnień administratora.";
            }

            return RedirectToAction(nameof(Users));
        }
    }


    public class AdminUserListVM
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsDriver { get; set; }
    }

    public class AdminDashboardVM
    {
        public int TotalDrivers { get; set; }
        public int TotalPassengers { get; set; }

        public List<string> Last7DaysLabels { get; set; } = new List<string>();
        public List<int> Last7DaysData { get; set; } = new List<int>();

        public List<CityStat> TopCities { get; set; } = new List<CityStat>();

        public List<string> DriverEarningsLabels { get; set; } = new List<string>();
        public List<decimal> DriverEarningsData { get; set; } = new List<decimal>();

        public List<RouteStat> PopularRoutes { get; set; } = new List<RouteStat>();
        public List<PassengerStat> TopPassengers { get; set; } = new List<PassengerStat>();
        public List<string> DaysOfWeekLabels { get; set; } = new List<string>();
        public List<int> DaysOfWeekData { get; set; } = new List<int>();

        public List<DriverAnalysisStat> AllDriversEarnings { get; set; } = new List<DriverAnalysisStat>();
    }

    public class CityStat { public string CityName { get; set; } public int RideCount { get; set; } }
    public class RouteStat { public string RouteName { get; set; } public int RideCount { get; set; } }
    public class PassengerStat { public string FullName { get; set; } public int RideCount { get; set; } }
    public class DriverAnalysisStat { public string DriverName { get; set; } public int CompletedRides { get; set; } public decimal TotalEarnings { get; set; } }
}
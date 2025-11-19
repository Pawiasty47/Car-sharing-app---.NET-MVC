using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    public class DriverProfileController : Controller //crud profil kierowcy
    {
        private static List<DriverProfile> _drivers = new List<DriverProfile>();

        public IActionResult Index()
        {
            return View(_drivers);
        }

        public IActionResult Details(Guid id)
        {
            var dp = _drivers.FirstOrDefault(x => x.UserId == id);
            if (dp == null) return NotFound();
            return View(dp);
        }

        public IActionResult Edit(Guid id)
        {
            var dp = _drivers.FirstOrDefault(x => x.UserId == id);
            if (dp == null) return NotFound();
            return View(dp);
        }

        [HttpPost]
        public IActionResult Edit(Guid id, DriverProfile model)
        {
            var dp = _drivers.FirstOrDefault(x => x.UserId == id);
            if (dp == null) return NotFound();

            dp.IsVerified = model.IsVerified;
            dp.DrivingLicenseImageUrl = model.DrivingLicenseImageUrl;
            dp.CompletedRidesCount = model.CompletedRidesCount;
            dp.Rating = model.Rating;

            TempData["SuccessMessage"] = "Profil kierowcy zapisany!";
            return RedirectToAction("Index");
        }
    }
}

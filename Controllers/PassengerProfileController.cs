using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    public class PassengerProfileController : Controller //crud profil pasazera
    {
        private static List<PassengerProfile> _passengers = new List<PassengerProfile>();

        public IActionResult Index()
        {
            return View(_passengers);
        }

        public IActionResult Details(Guid id)
        {
            var p = _passengers.FirstOrDefault(x => x.UserId == id);
            if (p == null) return NotFound();
            return View(p);
        }

        public IActionResult Edit(Guid id)
        {
            var p = _passengers.FirstOrDefault(x => x.UserId == id);
            if (p == null) return NotFound();
            return View(p);
        }

        [HttpPost]
        public IActionResult Edit(Guid id, PassengerProfile model)
        {
            var p = _passengers.FirstOrDefault(x => x.UserId == id);
            if (p == null) return NotFound();

            p.Rating = model.Rating;
            p.CompletedBookingsCount = model.CompletedBookingsCount;
            p.PrefersNonSmoking = model.PrefersNonSmoking;
            p.PrefersQuietRide = model.PrefersQuietRide;

            TempData["SuccessMessage"] = "Profil pasażera zapisany!";
            return RedirectToAction("Index");
        }
    }
}

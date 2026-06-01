using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    public class ReviewController : Controller //crud opinie
    {
        private static List<Review> _reviews = new List<Review>();

        public IActionResult Index()
        {
            return View(_reviews);
        }

        public IActionResult Details(Guid id)
        {
            var r = _reviews.FirstOrDefault(x => x.Id == id);
            if (r == null) return NotFound();
            return View(r);
        }

        public IActionResult Create()
        {
            return View(new Review());
        }

        [HttpPost]
        public IActionResult Create(Review model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Id = Guid.NewGuid();
            model.CreateAt = DateTime.UtcNow;
            _reviews.Add(model);

            TempData["SuccessMessage"] = "Opinia dodana!";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(Guid id)
        {
            var r = _reviews.FirstOrDefault(x => x.Id == id);
            if (r == null) return NotFound();
            return View(r);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var r = _reviews.FirstOrDefault(x => x.Id == id);
            if (r == null) return NotFound();

            _reviews.Remove(r);
            TempData["SuccessMessage"] = "Opinia usunięta!";
            return RedirectToAction("Index");
        }

        public static void ClearReviews()
        {
            _reviews.Clear();
        }
    }
}

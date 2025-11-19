using Microsoft.AspNetCore.Mvc;
using projekt_zespołowy.Models;

namespace projekt_zespołowy.Controllers
{
    public class BookingController : Controller //narazie podstawowy crud dla booking
    {
        private static List<Booking> _bookings = new List<Booking>();

        public IActionResult Index()
        {
            return View(_bookings);
        }

        public IActionResult Details(Guid id)
        {
            var b = _bookings.FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        public IActionResult Create()
        {
            return View(new Booking());
        }

        [HttpPost]
        public IActionResult Create(Booking model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            _bookings.Add(model);

            TempData["SuccessMessage"] = "Rezerwacja utworzona!";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(Guid id)
        {
            var b = _bookings.FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        [HttpPost]
        public IActionResult Edit(Guid id, Booking model)
        {
            var b = _bookings.FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();

            b.Status = model.Status;
            b.PaymentStatus = model.PaymentStatus;
            b.SeatsRequested = model.SeatsRequested;
            b.CommentByPassenger = model.CommentByPassenger;
            b.CommentByDriver = model.CommentByDriver;

            TempData["SuccessMessage"] = "Rezerwacja zaktualizowana!";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(Guid id)
        {
            var b = _bookings.FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();
            return View(b);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var b = _bookings.FirstOrDefault(x => x.Id == id);
            if (b == null) return NotFound();

            _bookings.Remove(b);
            TempData["SuccessMessage"] = "Rezerwacja usunięta!";
            return RedirectToAction("Index");
        }
    }
}

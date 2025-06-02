using HR_Products.Data;
using HR_Products.Models.Entitites;
using Microsoft.AspNetCore.Mvc;

namespace HR_Products.Controllers
{
    public class HolidayController : Controller
    {
        private readonly AppDbContext _context;

        public HolidayController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var holidays = _context.HOLIDAYS.OrderBy(h => h.HolidayDate).ToList();
            return View(holidays);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Holiday model)
        {
            if (ModelState.IsValid)
            {
                _context.HOLIDAYS.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Delete(int id)
        {
            var holiday = _context.HOLIDAYS.Find(id);
            if (holiday == null) return NotFound();

            _context.HOLIDAYS.Remove(holiday);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }

}

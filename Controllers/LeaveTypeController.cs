using Microsoft.AspNetCore.Mvc;
using HR_Products.Models.Entitites;
using System.Threading.Tasks;
using HR_Products.Data;
using Microsoft.EntityFrameworkCore;

namespace HR_Products.Controllers
{
    public class LeaveTypeController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employee/Index
        public async Task<IActionResult> Index()
        {
            var leavetype = await _context.LEAV_TYPE.ToListAsync();
            var leavetypeWithIndex = leavetype
                .Select((leavetype, index) => new { LeaveType = leavetype, Index = index + 1 })
                .ToList();

            return View(leavetype);
        }

        // GET: Employee/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        public async Task<IActionResult> Create(LeaveType leavetype)
        {
            // Check if a leave type with the same name already exists.
            var existingLeaveType = await _context.LEAV_TYPE
                .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_NAME == leavetype.LEAV_TYPE_NAME);

            if (existingLeaveType != null)
        {
          ModelState.AddModelError("LEAV_TYPE_NAME", "A leave type with this name already exists.");
          return View(leavetype); // This should trigger the display of errors
        }

        if (!ModelState.IsValid)
        {
          return View(leavetype);
        }

            leavetype.CreatedAt = DateTime.Now;
            leavetype.UpdatedAt = DateTime.Now;

            _context.LEAV_TYPE.Add(leavetype);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var leavetype = await _context.LEAV_TYPE.FindAsync(id);
            if (leavetype == null)
            {
                return NotFound();
            }

            return View(leavetype);
        }

        // POST: Employee/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeaveType leavetype)
        {
            if (!ModelState.IsValid)
            {
                return View(leavetype);
            }

            var leaveType = await _context.LEAV_TYPE.FindAsync(id);
            if (leaveType == null)
            {
                return NotFound();
            }

            // Update only the non-key fields
            leaveType.LEAV_TYPE_NAME = leavetype.LEAV_TYPE_NAME;
            leaveType.DESCRIPTION = leavetype.DESCRIPTION;
            leaveType.IS_PAID = leavetype.IS_PAID;
            leaveType.DEFAULT_DAY_PER_YEAR = leavetype.DEFAULT_DAY_PER_YEAR;
            leaveType.ACCRUAL_METHOD = leavetype.ACCRUAL_METHOD;
            leaveType.CARRY_OVER_LIMIT = leavetype.CARRY_OVER_LIMIT;
            leaveType.IS_ACTIVE = leavetype.IS_ACTIVE;
            leaveType.REQUIRE_APPROVAL = leavetype.REQUIRE_APPROVAL;
            leaveType.ATTACH_REQUIRE = leavetype.ATTACH_REQUIRE;
            leaveType.GENDER_SPECIFIC = leavetype.GENDER_SPECIFIC;
            leaveType.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var leavetype = await _context.LEAV_TYPE.FindAsync(id);
            if (leavetype == null)
            {
                return NotFound();
            }

            _context.LEAV_TYPE.Remove(leavetype);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.LEAV_TYPE.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }
    }
}
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

        public async Task<IActionResult> Index()
        {
            var leavetype = await _context.LEAV_TYPE.ToListAsync();
            var leavetypeWithIndex = leavetype
                .Select((leavetype, index) => new { LeaveType = leavetype, Index = index + 1 })
                .ToList();

            return View(leavetype);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveType leavetype)
        {
            var existingLeaveType = await _context.LEAV_TYPE
                .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_NAME == leavetype.LEAV_TYPE_NAME);

            if (existingLeaveType != null)
        {
          ModelState.AddModelError("LEAV_TYPE_NAME", "A leave type with this name already exists.");
          return View(leavetype); 
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

        [HttpGet] 
        public async Task<IActionResult> Delete(int id)
        {
            var leavetype = await _context.LEAV_TYPE.FindAsync(id);
            if (leavetype == null)
            {
                TempData["ErrorMessage"] = "Leave Type not found."; 
                return RedirectToAction("Index");
            }

            bool isReferenced = await _context.LEAV_REQUESTS.AnyAsync(lr => lr.LeaveTypeId == leavetype.LEAV_TYPE_ID);

            if (isReferenced)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{leavetype.LEAV_TYPE_NAME}'. There are existing leave requests associated with this leave type.";
                return RedirectToAction("Index"); 
            }

            try
            {
                _context.LEAV_TYPE.Remove(leavetype);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Leave Type '{leavetype.LEAV_TYPE_NAME}' deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the leave type: {ex.Message}";
                
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

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
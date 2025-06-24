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
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create(LeaveType leavetype)
        {
            var existingLeaveType = await _context.LEAV_TYPE
                .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_NAME == leavetype.LEAV_TYPE_NAME);

            if (existingLeaveType != null)
            {
                ModelState.AddModelError("LEAV_TYPE_NAME", "A leave type with this name already exists.");
                return View(leavetype);
            }

            leavetype.IS_PAID = (leavetype.IS_PAID == "Y") ? "Y" : "N";
            leavetype.IS_ACTIVE = (leavetype.IS_ACTIVE == "Y") ? "Y" : "N";
            leavetype.REQUIRE_APPROVAL = (leavetype.REQUIRE_APPROVAL == "Y") ? "Y" : "N";
            leavetype.ATTACH_REQUIRE = (leavetype.ATTACH_REQUIRE == "Y") ? "Y" : "N";


            if (!ModelState.IsValid)
            {
                return View(leavetype);
            }

            leavetype.CreatedAt = DateTime.Now;
            leavetype.UpdatedAt = DateTime.Now; 

            _context.LEAV_TYPE.Add(leavetype);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Leave type created successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Leave Type ID not provided.";
                return RedirectToAction(nameof(Index));
            }

            var leaveType = await _context.LEAV_TYPE.FindAsync(id);
            if (leaveType == null)
            {
                TempData["ErrorMessage"] = "Leave Type not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeaveType leavetype) 
        {
            if (id != leavetype.LEAV_TYPE_ID)
            {
                TempData["ErrorMessage"] = "Mismatched Leave Type ID.";
                return RedirectToAction(nameof(Index));
            }

            
            leavetype.IS_PAID = (leavetype.IS_PAID == "Y") ? "Y" : "N";
            leavetype.IS_ACTIVE = (leavetype.IS_ACTIVE == "Y") ? "Y" : "N";
            leavetype.REQUIRE_APPROVAL = (leavetype.REQUIRE_APPROVAL == "Y") ? "Y" : "N";
            leavetype.ATTACH_REQUIRE = (leavetype.ATTACH_REQUIRE == "Y") ? "Y" : "N";

            if (!ModelState.IsValid)
            {
                return View(leavetype);
            }

            var leaveTypeToUpdate = await _context.LEAV_TYPE.FindAsync(id);
            if (leaveTypeToUpdate == null)
            {
                TempData["ErrorMessage"] = "Leave Type not found for update.";
                return RedirectToAction(nameof(Index));
            }

            leaveTypeToUpdate.LEAV_TYPE_NAME = leavetype.LEAV_TYPE_NAME;
            leaveTypeToUpdate.DESCRIPTION = leavetype.DESCRIPTION;
            leaveTypeToUpdate.IS_PAID = leavetype.IS_PAID; 
            leaveTypeToUpdate.DEFAULT_DAY_PER_YEAR = leavetype.DEFAULT_DAY_PER_YEAR;
            leaveTypeToUpdate.ACCRUAL_METHOD = leavetype.ACCRUAL_METHOD;
            leaveTypeToUpdate.CARRY_OVER_LIMIT = leavetype.CARRY_OVER_LIMIT;
            leaveTypeToUpdate.IS_ACTIVE = leavetype.IS_ACTIVE; 
            leaveTypeToUpdate.REQUIRE_APPROVAL = leavetype.REQUIRE_APPROVAL; 
            leaveTypeToUpdate.ATTACH_REQUIRE = leavetype.ATTACH_REQUIRE; 
            leaveTypeToUpdate.GENDER_SPECIFIC = leavetype.GENDER_SPECIFIC;
            leaveTypeToUpdate.UpdatedAt = DateTime.Now;

            try
            {
                _context.Update(leaveTypeToUpdate); 
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Leave type updated successfully!";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LEAV_TYPE.Any(e => e.LEAV_TYPE_ID == id))
                {
                    TempData["ErrorMessage"] = "Leave type no longer exists.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating leave type: {ex.Message}");
                return View(leavetype); 
            }
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
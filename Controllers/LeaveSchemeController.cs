using HR_Products.Data;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace HR_Products.Controllers
{
    public class LeaveSchemeController : Controller
    {
        private readonly AppDbContext _context;

        public LeaveSchemeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var schemes = _context.LEAV_SCHEME.ToList();
            return View(schemes); 
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Leavescheme scheme)
        {
            var existingScheme = await _context.LEAV_SCHEME
                .FirstOrDefaultAsync(x => x.SCHEME_NAME == scheme.SCHEME_NAME);

            if (existingScheme != null)
            {
                ModelState.AddModelError("SCHEME_NAME", "A leave scheme with this name already exists.");
                return View(scheme); 
            }

            scheme.CREATED_AT = DateTime.Now;
            scheme.UPDATED_AT = DateTime.Now;

            _context.LEAV_SCHEME.Add(scheme);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var leavetype = await _context.LEAV_SCHEME.FindAsync(id);
            if (leavetype == null)
            {
                return NotFound();
            }

            return View(leavetype);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Leavescheme scheme)
        {

            var leaveschm = await _context.LEAV_SCHEME.FindAsync(id);
            if (leaveschm == null)
            {
                return NotFound();
            }

            leaveschm.SCHEME_NAME = scheme.SCHEME_NAME;
            leaveschm.DESCRIPTION = scheme.DESCRIPTION;
            leaveschm.UPDATED_AT = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // GET: Employee/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveschm = await _context.LEAV_SCHEME.FindAsync(id);
            if (leaveschm == null)
            {
                return NotFound();
            }

            _context.LEAV_SCHEME.Remove(leaveschm);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var leaveScheme = await _context.LEAV_SCHEME.FindAsync(id);
            if (leaveScheme == null)
            {
                return NotFound();
            }

            var leaveSchemeTypes = await _context.LEAV_SCHEME_TYPE
                .Where(x => x.SCHEME_ID == id)
            .ToListAsync();

            var viewModel = new LeaveSchemeDetailsViewModel
            {
                LeaveScheme = leaveScheme,
                LeaveSchemeTypes = leaveSchemeTypes
            };

            return View(viewModel);
        }

        // Add LEAV_SCHEME_TYPE
        [HttpPost]
        public async Task<IActionResult> AddSchemeType(Leaveschemetype schemetype)
        {
            var type = new Leaveschemetype
            {
                SCHEME_ID = schemetype.SCHEME_ID,
                LEAVE_TYPE_ID = schemetype.LEAVE_TYPE_ID,
                CREATED_AT = DateTime.Now,
                UPDATED_AT = DateTime.Now
            };

            _context.LEAV_SCHEME_TYPE.Add(type);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = schemetype.SCHEME_ID });
        }

        [HttpPost]
        public async Task<IActionResult> AddSchemeTypeDetail(Leaveschemetypedetl typedetl)
        {
            var detl = new Leaveschemetypedetl
            {
                TYPE_ID = typedetl.TYPE_ID,
                FROM_YEAR = typedetl.FROM_YEAR,
                TO_YEAR = typedetl.TO_YEAR,
                DAYS_PER_YEAR = typedetl.DAYS_PER_YEAR,
                CREATED_AT = DateTime.Now,
                UPDATED_AT = DateTime.Now
            };

            _context.LEAV_SCHEME_TYPE_DETL.Add(detl);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "LeaveSchemeType", new { id = typedetl.TYPE_ID });
        }


        [HttpGet]
        public async Task<IActionResult> CreateDetail(int id)
        {
            var leaveScheme = await _context.LEAV_SCHEME.FindAsync(id);
            if (leaveScheme == null)
            {
                return NotFound();
            }

            var leaveTypes = await _context.LEAV_TYPE
                .Select(x => new SelectListItem
                {
                    Value = x.LEAV_TYPE_ID.ToString(),
                    Text = x.LEAV_TYPE_NAME
                })
                .ToListAsync();

            var viewModel = new LeaveSchemeTypeFormViewModel
            {
                LeaveSchemeType = new Leaveschemetype
                {
                    SCHEME_ID = leaveScheme.SCHEME_ID,
                    SCHEME_NAME = leaveScheme.SCHEME_NAME
                },
                LeaveTypes = leaveTypes
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CreateDetail(LeaveSchemeTypeFormViewModel viewModel)
        {
            var scheme = await _context.LEAV_SCHEME
                .FirstOrDefaultAsync(x => x.SCHEME_ID == viewModel.LeaveSchemeType.SCHEME_ID);

            if (scheme == null)
            {
                return NotFound("Scheme not found.");
            }

            var leaveType = await _context.LEAV_TYPE
                .FirstOrDefaultAsync(x => x.LEAV_TYPE_ID == viewModel.LeaveSchemeType.LEAVE_TYPE_ID);

            if (leaveType == null)
            {
                return NotFound("Leave Type not found.");
            }
            var existingLeaveType = await _context.LEAV_SCHEME_TYPE
                .FirstOrDefaultAsync(x => x.SCHEME_ID == viewModel.LeaveSchemeType.SCHEME_ID &&
                                         x.LEAVE_TYPE_NAME == leaveType.LEAV_TYPE_NAME);

            if (existingLeaveType != null)
            {
                ModelState.AddModelError("LeaveSchemeType.LEAVE_TYPE_ID", "This leave type is already added to this scheme.");
                
                viewModel.LeaveTypes = await _context.LEAV_TYPE
                    .Select(lt => new SelectListItem
                    {
                        Value = lt.LEAV_TYPE_ID.ToString(),
                        Text = lt.LEAV_TYPE_NAME
                    })
                    .ToListAsync();
                return View(viewModel); 
            }

            var type = new Leaveschemetype
            {
                SCHEME_ID = viewModel.LeaveSchemeType.SCHEME_ID,
                LEAVE_TYPE_ID = viewModel.LeaveSchemeType.LEAVE_TYPE_ID,
                SCHEME_NAME = scheme.SCHEME_NAME,
                LEAVE_TYPE_NAME = leaveType.LEAV_TYPE_NAME,
                CREATED_AT = DateTime.Now,
                UPDATED_AT = DateTime.Now
            };

            _context.LEAV_SCHEME_TYPE.Add(type);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = viewModel.LeaveSchemeType.SCHEME_ID });
        }

      


        [HttpGet]
        public async Task<IActionResult> Detailstype(int id, string leaveTypeName) 
        {
            
            var selectedType = await _context.LEAV_SCHEME_TYPE
                .FirstOrDefaultAsync(t => t.TYPE_ID == id);

            if (selectedType == null)
                return NotFound();

            var leaveScheme = await _context.LEAV_SCHEME
                .FirstOrDefaultAsync(s => s.SCHEME_ID == selectedType.SCHEME_ID);

            if (leaveScheme == null)
                return NotFound();

            
            var schemeTypes = await _context.LEAV_SCHEME_TYPE
                .Where(t => t.SCHEME_ID == leaveScheme.SCHEME_ID)
                .ToListAsync();

            var schemeTypeDetl = await _context.LEAV_SCHEME_TYPE_DETL
                .Where(d => d.TYPE_ID == id)
                .ToListAsync();
            var viewModel = new LeaveSchemeDetailsViewModel
            {
                LeaveScheme = leaveScheme,
                LeaveSchemeTypes = schemeTypes,
                LeaveSchemeTypeDetl = schemeTypeDetl
            };

            ViewBag.InitialLeaveTypeName = leaveTypeName;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateDetlType(int schemeId, string leaveTypeName)
        {
            var leaveScheme = await _context.LEAV_SCHEME.FindAsync(schemeId);
            if (leaveScheme == null)
            {
                return NotFound();
            }

            var viewModel = new LeaveSchemeTypeFormViewModel
            {
                LeaveScheme = leaveScheme,
                LeaveSchemeType = new Leaveschemetype { LEAVE_TYPE_NAME = leaveTypeName },
                SCHEME_ID = schemeId,
                UPDATED_AT = DateTime.Now
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDetlType(LeaveSchemeTypeFormViewModel viewModel)
        {
            var leaveScheme = await _context.LEAV_SCHEME.FindAsync(viewModel.SCHEME_ID);

            if (leaveScheme == null)
            {
                return NotFound();
            }

            var leaveType = await _context.LEAV_SCHEME_TYPE
                .FirstOrDefaultAsync(lst => lst.LEAVE_TYPE_NAME == viewModel.LeaveSchemeType.LEAVE_TYPE_NAME && lst.SCHEME_ID == viewModel.SCHEME_ID);

            if (leaveType == null)
            {
                ModelState.AddModelError("LeaveSchemeType.LEAVE_TYPE_NAME", "Invalid Leave Type Name for this scheme.");
                viewModel.LeaveScheme = leaveScheme;
                return View(viewModel);
            }

            // Check for existing duplicate record
            var existingDetail = await _context.LEAV_SCHEME_TYPE_DETL.FirstOrDefaultAsync(
                d => d.SCHEME_ID == viewModel.SCHEME_ID &&
                     d.TYPE_ID == leaveType.TYPE_ID &&
                     d.FROM_YEAR == viewModel.FROM_YEAR &&
                     d.TO_YEAR == viewModel.TO_YEAR);

            if (existingDetail != null)
            {
                ModelState.AddModelError("Leaveschemetypedetl.LEAVE_TYPE_NAME", "A detail record with the same Leave Type, From Year, and To Year already exists for this scheme.");
                viewModel.LeaveScheme = leaveScheme;
                return View(viewModel);
            }

            var newDetail = new Leaveschemetypedetl
            {
                TYPE_ID = leaveType.TYPE_ID,
                SCHEME_ID = viewModel.SCHEME_ID,
                SCHEME_NAME = leaveScheme.SCHEME_NAME,
                LEAVE_TYPE_NAME = viewModel.LeaveSchemeType.LEAVE_TYPE_NAME,
                FROM_YEAR = viewModel.FROM_YEAR,
                TO_YEAR = viewModel.TO_YEAR,
                DAYS_PER_YEAR = viewModel.DAYS_PER_YEAR,
                UPDATED_AT = DateTime.Now,
                CREATED_AT = DateTime.Now
            };

            _context.LEAV_SCHEME_TYPE_DETL.Add(newDetail);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = viewModel.SCHEME_ID });
        }

        [HttpGet]
        public async Task<IActionResult> Deleteschtype(int id)
        {
            var leaveTypeToDelete = await _context.LEAV_SCHEME_TYPE.FindAsync(id);

            if (leaveTypeToDelete == null)
            {
                return NotFound();
            }

            // Assuming LEAV_SCHEME_TYPE has a SCHEME_ID property
            int schemeId = leaveTypeToDelete.SCHEME_ID;

            _context.LEAV_SCHEME_TYPE.Remove(leaveTypeToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = schemeId });
        }


        [HttpGet]
        public async Task<IActionResult> Deleteschtypedtl(int id)
        {
            var detailToDelete = await _context.LEAV_SCHEME_TYPE_DETL.FindAsync(id);
            if (detailToDelete == null)
            {
                return NotFound();
            }

            int schemeId = detailToDelete.SCHEME_ID; // Get the SCHEME_ID

            _context.LEAV_SCHEME_TYPE_DETL.Remove(detailToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = schemeId });
        }
    }
}

using HR_Products.Data;
using HR_Products.Models.Entities;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;


namespace HR_Products.Controllers
{
    [Authorize]
    public class LeaveApprovalController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly AppDbContext _context;

        public LeaveApprovalController(IWebHostEnvironment hostEnvironment, AppDbContext context)
        {
            _hostEnvironment = hostEnvironment;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return Forbid();
            }

            var employee = await _context.EMPE_PROFILE
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return Forbid();
            }

            var pendingRequests = await _context.LEAV_REQUESTS
                .Include(lr => lr.Employee)
                .Include(lr => lr.LeaveType)
                .Where(lr => lr.ApproverName == employee.EmpeName && lr.Status == "Pending")
                .Select(lr => new LeaveApprovalViewModel
                {
                    Id = lr.Id,
                    EmployeeName = lr.EmpeName,
                    LeaveTypeName = lr.LeaveTypeName,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    DurationType = lr.DurationType,
                    Duration = lr.Duration,
                    Reason = lr.Reason,
                    RequestedAt = lr.RequestedAt,
                    AttachmentPath = lr.AttachmentPath,
                    AttachmentFileName = lr.AttachmentFileName,
                    AttachmentContentType = lr.AttachmentContentType
                })
                .ToListAsync();

            return View(pendingRequests);
        }

        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var leaveRequest = await _context.LEAV_REQUESTS.FindAsync(id);

            if (leaveRequest == null || string.IsNullOrEmpty(leaveRequest.AttachmentPath))
            {
                return NotFound(); 
            }

            var webRootPath = _hostEnvironment.WebRootPath; 
            var filePath = Path.Combine(webRootPath, leaveRequest.AttachmentPath.TrimStart('/')); // Remove leading slash

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(); 
            }

            return PhysicalFile(filePath, leaveRequest.AttachmentContentType, leaveRequest.AttachmentFileName);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.LEAV_REQUESTS
                    .Include(lr => lr.LeaveType)
                    .FirstOrDefaultAsync(lr => lr.Id == id);
                var allApproved = await _context.LEAV_REQUESTS
                    .Where(lr => lr.EmployeeId == request.EmployeeId
                              && lr.LeaveTypeId == request.LeaveTypeId
                              && lr.Status == "Approved")
                    .ToListAsync();

                decimal totalUsed = allApproved.Sum(lr => lr.Duration) + request.Duration;
                decimal newBalance = request.LeaveType.DEFAULT_DAY_PER_YEAR - totalUsed;

                request.Status = "Approved";
                request.ApprovedAt = DateTime.Now;
                request.UsedToDate = totalUsed;
                request.AccrualBalance = newBalance;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Leave request successfully Approved.";
                return RedirectToAction("Index");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _context.LEAV_REQUESTS.FindAsync(id);

            if (leaveRequest == null)
            {
                return NotFound();
            }
            leaveRequest.Status = "Rejected";
            leaveRequest.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Leave request successfully Rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult ApproveLeave(int id)
        {
            var leaveRequest = _context.LEAV_REQUESTS.Find(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.Status = "Approved";
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Leave request has been approved successfully!";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public IActionResult RejectLeave(int id)
        {
            var leaveRequest = _context.LEAV_REQUESTS.Find(id);
            if (leaveRequest == null)
            {
                return NotFound();
            }

            leaveRequest.Status = "Rejected";
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Leave request has been rejected.";
            return RedirectToAction("Details", new { id = id });
        }

    }
}
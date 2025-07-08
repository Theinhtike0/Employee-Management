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
using HR_Products.Services;


namespace HR_Products.Controllers
{
    [Authorize]
    public class LeaveApprovalController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        public LeaveApprovalController(IWebHostEnvironment hostEnvironment, AppDbContext context, IEmailService emailService)
        {
            _hostEnvironment = hostEnvironment;
            _context = context;
            _emailService = emailService;
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
                    .Include(lr => lr.Employee) 
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Leave request not found.";
                    return RedirectToAction("Index"); 
                }

                if (request.Status == "Approved")
                {
                    TempData["WarningMessage"] = "Leave request is already approved.";
                    return RedirectToAction("Index"); 
                }

                var allApproved = await _context.LEAV_REQUESTS
                    .Where(lr => lr.EmployeeId == request.EmployeeId
                                && lr.LeaveTypeId == request.LeaveTypeId
                                && lr.Status == "Approved"
                                && lr.StartDate.Year == request.StartDate.Year) 
                    .ToListAsync();

                decimal currentYearApprovedUsed = allApproved.Sum(lr => lr.Duration);
                decimal totalUsedAfterApproval = currentYearApprovedUsed + request.Duration;


                var leaveBalance = await _context.LEAV_BALANCE
                    .FirstOrDefaultAsync(lb => lb.EmpeId == request.EmployeeId
                                            && lb.LeaveTypeId == request.LeaveTypeId
                                            && lb.Year == request.StartDate.Year);

                decimal initialDefaultBalance = request.LeaveType.DEFAULT_DAY_PER_YEAR; 

                if (leaveBalance != null)
                {
                    leaveBalance.Balance = (int)(initialDefaultBalance - totalUsedAfterApproval);
                    _context.LEAV_BALANCE.Update(leaveBalance);
                }
                else
                {
                    
                    var newBalanceEntry = new LeaveBalance
                    {
                        EmpeId = request.EmployeeId,
                        LeaveTypeId = request.LeaveTypeId,
                        Balance = (int)(initialDefaultBalance - totalUsedAfterApproval),
                        Year = request.StartDate.Year,
                        EmpeName = request.EmpeName, 
                        LeaveTypeName = request.LeaveTypeName, 
                        CreatedDate = DateTime.Now
                    };
                    _context.LEAV_BALANCE.Add(newBalanceEntry);
                }
                decimal newBalanceValueForRequest = initialDefaultBalance - totalUsedAfterApproval; 

                request.Status = "Approved";
                request.ApprovedAt = DateTime.Now;
                request.UsedToDate = totalUsedAfterApproval; 
                request.AccrualBalance = newBalanceValueForRequest; 

                _context.LEAV_REQUESTS.Update(request); 

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Leave request successfully Approved.";
                

                if (request.Employee != null && !string.IsNullOrEmpty(request.Employee.Email))
                {
                    string recipientEmail = request.Employee.Email; 
                    string subject = $"Your Leave Request Has Been Approved - {request.LeaveTypeName}";
                    string message = $"Dear {request.Employee.EmpeName},<br/><br/>" +
                                     $"Your leave request for <b>{request.LeaveTypeName}</b> from <b>{request.StartDate.ToString("MM/dd/yyyy")}</b> to <b>{request.EndDate.ToString("MM/dd/yyyy")}</b> has been **APPROVED**.<br/><br/>" +
                                     $"<b>Reason:</b> {request.Reason}<br/>" +
                                     $"<b>Approved Duration:</b> {request.Duration} {request.DurationType}<br/>" +
                                     $"<b>Your Remaining {request.LeaveTypeName} Balance:</b> {request.AccrualBalance} days<br/><br/>" +
                                     $"Best regards,<br/>" +
                                     $"The HR Products Team";

                    var approverProfile = GetCurrentUser(); 
                    string senderEmailFromProfile = approverProfile?.Email;
                    string senderNameFromProfile = approverProfile?.EmpeName;

                    
                    string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                    string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                    try
                    {
                        await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                        TempData["SuccessMessage"] += " Email notification sent to employee!";
                        
                    }
                    catch (Exception emailEx)
                    {
                        
                        TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to employee failed.";
                    }
                }
                else
                {
                    
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Employee email not found for approval notification.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                TempData["ErrorMessage"] = "An error occurred while approving the leave request. Please try again.";
                
                return RedirectToAction("Index"); 
            }
        }

        private EmployeeProfile GetCurrentUser()
        {
            var userEmail = HttpContext.User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var employees = _context.EMPE_PROFILE.Where(e => e.Email != null).ToList();
            var employee = employees.FirstOrDefault(e =>
                e.Email.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

            if (employee == null)
            {
                System.Diagnostics.Debug.WriteLine($"No employee found for email: {userEmail}");
                throw new InvalidOperationException($"Employee not found for email: {userEmail}");
            }

            return employee;
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var leaveRequest = await _context.LEAV_REQUESTS
                    .Include(lr => lr.Employee) 
                    .Include(lr => lr.LeaveType) 
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (leaveRequest == null)
                {
                    
                    TempData["ErrorMessage"] = "Leave request not found.";
                    return NotFound(); 
                }

                if (leaveRequest.Status == "Rejected")
                {
                    
                    TempData["WarningMessage"] = "Leave request is already rejected.";
                    return RedirectToAction(nameof(Index));
                }

                leaveRequest.Status = "Rejected";
                leaveRequest.ApprovedAt = DateTime.Now; 

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); 

                TempData["SuccessMessage"] = "Leave request successfully Rejected.";
                

                if (leaveRequest.Employee != null && !string.IsNullOrEmpty(leaveRequest.Employee.Email))
                {
                    string recipientEmail = leaveRequest.Employee.Email; 
                    string subject = $"Your Leave Request Has Been Rejected - {leaveRequest.LeaveTypeName}";
                    string message = $"Dear {leaveRequest.Employee.EmpeName},<br/><br/>" +
                                     $"We regret to inform you that your leave request for <b>{leaveRequest.LeaveTypeName}</b> " +
                                     $"from <b>{leaveRequest.StartDate.ToString("MM/dd/yyyy")}</b> to <b>{leaveRequest.EndDate.ToString("MM/dd/yyyy")}</b> has been **REJECTED**.<br/><br/>" +
                                     $"<b>Reason provided:</b> {leaveRequest.Reason}<br/>" + 
                                                                                             
                                     $"<br/>Please contact HR for further details or clarification.<br/><br/>" +
                                     $"Best regards,<br/>" +
                                     $"The HR Products Team";

                    var approverProfile = GetCurrentUser(); 
                    string senderEmailFromProfile = approverProfile?.Email;
                    string senderNameFromProfile = approverProfile?.EmpeName;

                    string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                    string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                    try
                    {
                        await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                        TempData["SuccessMessage"] += " Email notification sent to employee!";
                        
                    }
                    catch (Exception emailEx)
                    {
                        
                        TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to employee failed.";
                    }
                }
                else
                {
                    
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Employee email not found for rejection notification.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); 
                TempData["ErrorMessage"] = "An error occurred while rejecting the leave request. Please try again.";
                return RedirectToAction(nameof(Index)); 
            }
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
using HR_Products.Data;
using HR_Products.Models.Entities;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using HR_Products.Services;

namespace HR_Products.Controllers
{
    public class LeaveRequestsController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService; // Declare IEmailService


        public LeaveRequestsController(IWebHostEnvironment hostEnvironment, AppDbContext context, IEmailService emailService)
        {
            _hostEnvironment = hostEnvironment;
            _context = context;
            _emailService = emailService;
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = GetCurrentUser();
            var isAdmin = HttpContext.User.IsInRole("Admin") || HttpContext.User.IsInRole("HR-Admin");

            ViewBag.Employees = await _context.EMPE_PROFILE
                .Select(e => new { e.EmpeId, e.EmpeName })
                .ToListAsync();
            ViewBag.LeaveTypes = await _context.LEAV_TYPE
                .Select(lt => new { lt.LEAV_TYPE_ID, lt.LEAV_TYPE_NAME, lt.DESCRIPTION })
                .ToListAsync();
            ViewBag.DurationTypes = new List<string> { "Full-Day", "AM", "PM" };

            var model = new LeaveRequestViewModel();

            if (!isAdmin && currentUser != null)
            {
                model.EmployeeId = currentUser.EmpeId;
                model.EmpeName = currentUser.EmpeName; 
            }

            ViewBag.IsAdmin = isAdmin;
            return View(model);
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(LeaveRequestViewModel viewModel)
        //{
        //    try
        //    {
        //        var leaveType = await _context.LEAV_TYPE
        //            .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_ID == viewModel.LeaveTypeId);

        //        if (leaveType == null)
        //        {
        //            ModelState.AddModelError("LeaveTypeId", "Invalid leave type selected");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel));
        //        }

        //        if (viewModel.AttachmentFile != null)
        //        {
        //            if (viewModel.AttachmentFile.Length > 5 * 1024 * 1024) // 5MB
        //            {
        //                ModelState.AddModelError("AttachmentFile", "File size must be less than 5MB");
        //                await PopulateViewBags();
        //                return HandleResponse(View(viewModel));
        //            }

        //            var allowedExtensions = new[] { ".pdf", ".jpg", ".png", ".doc", ".docx" };
        //            var fileExtension = Path.GetExtension(viewModel.AttachmentFile.FileName).ToLower();
        //            if (!allowedExtensions.Contains(fileExtension))
        //            {
        //                ModelState.AddModelError("AttachmentFile", "Only PDF, JPG, PNG, DOC and DOCX files are allowed");
        //                await PopulateViewBags();
        //                return HandleResponse(View(viewModel));
        //            }
        //        }

        //        if (leaveType.LEAV_TYPE_NAME != "CL" && (viewModel.ApprovedById == null || viewModel.ApprovedById == 0))
        //        {
        //            ModelState.AddModelError("", "No approver was assigned for this leave type");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel));
        //        }
        //        var employee = await _context.EMPE_PROFILE
        //            .FirstOrDefaultAsync(e => e.EmpeId == viewModel.EmployeeId);

        //        if (employee == null)
        //        {
        //            ModelState.AddModelError("EmployeeId", "Selected employee not found");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel));
        //        }

        //        var existingLeave = await _context.LEAV_REQUESTS
        //            .AnyAsync(lr => lr.EmployeeId == viewModel.EmployeeId
        //            && ((viewModel.StartDate >= lr.StartDate && viewModel.StartDate <= lr.EndDate)
        //                || (viewModel.EndDate >= lr.StartDate && viewModel.EndDate <= lr.EndDate)
        //                || (lr.StartDate >= viewModel.StartDate && lr.StartDate <= viewModel.EndDate))
        //            && lr.Status == "Approved");

        //        if (existingLeave)
        //        {
        //            ModelState.AddModelError("", "You already have an approved leave for these dates. Choose other days.");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel), "Conflict");
        //        }

        //        var holidays = await _context.HOLIDAYS
        //            .Where(h => h.HolidayDate >= viewModel.StartDate && h.HolidayDate <= viewModel.EndDate)
        //            .ToListAsync();

        //        if (holidays.Any())
        //        {
        //            ModelState.AddModelError("", $"The selected date range includes holidays: {string.Join(", ", holidays.Select(h => h.HolidayDate.ToShortDateString()))}");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel), "Holiday Conflict");
        //        }

        //        viewModel.Duration = CalculateDuration(viewModel.StartDate, viewModel.EndDate, viewModel.DurationType);

        //        var currentUsedDays = await _context.LEAV_REQUESTS
        //            .Where(lr => lr.EmployeeId == viewModel.EmployeeId
        //                        && lr.LeaveTypeId == viewModel.LeaveTypeId
        //                        && lr.Status == "Approved")
        //            .SumAsync(lr => lr.Duration);

        //        var currentBalance = leaveType.DEFAULT_DAY_PER_YEAR - currentUsedDays;

        //        if (leaveType.LEAV_TYPE_NAME == "CL" && currentBalance < viewModel.Duration)
        //        {
        //            ModelState.AddModelError("", $"Not enough leave balance. Available: {currentBalance} days");
        //            await PopulateViewBags();
        //            return HandleResponse(View(viewModel), "Insufficient Balance");
        //        }

        //        string attachmentPath = null;
        //        byte[] attachmentFileData = null;

        //        if (viewModel.AttachmentFile != null && viewModel.AttachmentFile.Length > 0)
        //        {
        //            try
        //            {
        //                var uploadsDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "leave_attachments");
        //                Directory.CreateDirectory(uploadsDir);

        //                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(viewModel.AttachmentFile.FileName)}";
        //                var filePath = Path.Combine(uploadsDir, uniqueFileName);

        //                using (var stream = new FileStream(filePath, FileMode.Create))
        //                {
        //                    await viewModel.AttachmentFile.CopyToAsync(stream);
        //                }

        //                attachmentPath = $"/uploads/leave_attachments/{uniqueFileName}";
        //            }
        //            catch (Exception ex)
        //            {
        //                ModelState.AddModelError("AttachmentFile", $"Failed to save file: {ex.Message}");
        //                await PopulateViewBags();
        //                return HandleResponse(View(viewModel), "File Upload Error");
        //            }
        //        }

        //        var isAutoApproved = leaveType.LEAV_TYPE_NAME == "CL";
        //        var status = isAutoApproved ? "Approved" : "Pending";
        //        var approverName = isAutoApproved ? "Auto-Approved" : viewModel.ApproverName;

        //        var leaveRequest = new LeaveRequest
        //        {
        //            EmployeeId = viewModel.EmployeeId,
        //            Employee = employee,
        //            EmpeName = employee.EmpeName,
        //            LeaveTypeId = viewModel.LeaveTypeId,
        //            LeaveType = leaveType,
        //            LeaveTypeName = leaveType.LEAV_TYPE_NAME,
        //            StartDate = viewModel.StartDate,
        //            EndDate = viewModel.EndDate,
        //            DurationType = viewModel.DurationType,
        //            Duration = viewModel.Duration,
        //            Reason = viewModel.Reason,
        //            Status = status,
        //            RequestedAt = DateTime.Now,
        //            ApprovedAt = isAutoApproved ? DateTime.Now : (DateTime?)null,
        //            ApprovedById = isAutoApproved ? null : viewModel.ApprovedById,
        //            ApproverName = approverName,
        //            UsedToDate = isAutoApproved ? currentUsedDays + viewModel.Duration : currentUsedDays,
        //            AccrualBalance = isAutoApproved ? currentBalance - viewModel.Duration : currentBalance,
        //            OriginalUsedToDate = currentUsedDays,
        //            OriginalAccrualBalance = currentBalance,
        //            LeaveBalance = leaveType.DEFAULT_DAY_PER_YEAR,
        //            AttachmentFileName = viewModel.AttachmentFile?.FileName,
        //            AttachmentContentType = viewModel.AttachmentFile?.ContentType,
        //            AttachmentPath = attachmentPath,
        //            AttachmentFileData = attachmentFileData
        //        };

        //        _context.LEAV_REQUESTS.Add(leaveRequest);
        //        await _context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = $"Leave request submitted successfully!";
        //        return HandleResponse(RedirectToAction("Create"), status);
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", $"An error occurred while saving the leave request: {ex.Message}");
        //        await PopulateViewBags();
        //        return HandleResponse(View(viewModel), "Error");
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequestViewModel viewModel)
        {
            try
            {
                int currentYear = DateTime.Now.Year;

                var leaveType = await _context.LEAV_TYPE
                    .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_ID == viewModel.LeaveTypeId);

                if (leaveType == null)
                {
                    ModelState.AddModelError("LeaveTypeId", "Invalid leave type selected");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel));
                }

                if (viewModel.AttachmentFile != null)
                {
                    if (viewModel.AttachmentFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("AttachmentFile", "File size must be less than 5MB");
                        await PopulateViewBags();
                        return HandleResponse(View(viewModel));
                    }

                    var allowedExtensions = new[] { ".pdf", ".jpg", ".png", ".doc", ".docx" };
                    var fileExtension = Path.GetExtension(viewModel.AttachmentFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("AttachmentFile", "Only PDF, JPG, PNG, DOC and DOCX files are allowed");
                        await PopulateViewBags();
                        return HandleResponse(View(viewModel));
                    }
                }

                if (leaveType.LEAV_TYPE_NAME != "CL" && (viewModel.ApprovedById == null || viewModel.ApprovedById == 0))
                {
                    ModelState.AddModelError("", "No approver was assigned for this leave type");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel));
                }

                var employee = await _context.EMPE_PROFILE
                    .FirstOrDefaultAsync(e => e.EmpeId == viewModel.EmployeeId);

                if (employee == null)
                {
                    ModelState.AddModelError("EmployeeId", "Selected employee not found");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel));
                }

                var existingLeave = await _context.LEAV_REQUESTS
                    .AnyAsync(lr => lr.EmployeeId == viewModel.EmployeeId
                    && ((viewModel.StartDate >= lr.StartDate && viewModel.StartDate <= lr.EndDate)
                        || (viewModel.EndDate >= lr.StartDate && viewModel.EndDate <= lr.EndDate)
                        || (lr.StartDate >= viewModel.StartDate && lr.StartDate <= viewModel.EndDate))
                    && lr.Status == "Approved");

                if (existingLeave)
                {
                    ModelState.AddModelError("", "You already have an approved leave for these dates. Choose other days.");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel), "Conflict");
                }

                var holidays = await _context.HOLIDAYS
                    .Where(h => h.HolidayDate >= viewModel.StartDate && h.HolidayDate <= viewModel.EndDate)
                    .ToListAsync();

                if (holidays.Any())
                {
                    ModelState.AddModelError("", $"The selected date range includes holidays: {string.Join(", ", holidays.Select(h => h.HolidayDate.ToShortDateString()))}");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel), "Holiday Conflict");
                }

                viewModel.Duration = CalculateDuration(viewModel.StartDate, viewModel.EndDate, viewModel.DurationType);

                var currentUsedDays = await _context.LEAV_REQUESTS
                    .Where(lr => lr.EmployeeId == viewModel.EmployeeId
                                && lr.LeaveTypeId == viewModel.LeaveTypeId
                                && lr.Status == "Approved"
                                && lr.StartDate.Year == currentYear)
                    .SumAsync(lr => lr.Duration);

                var leaveBalance = await _context.LEAV_BALANCE
                    .FirstOrDefaultAsync(lb => lb.EmpeId == viewModel.EmployeeId
                                            && lb.LeaveTypeId == viewModel.LeaveTypeId
                                            && lb.Year == currentYear);

                decimal availableBalance;
                if (leaveBalance != null)
                {
                    availableBalance = leaveBalance.Balance - currentUsedDays;
                }
                else
                {
                    availableBalance = leaveType.DEFAULT_DAY_PER_YEAR - currentUsedDays;
                }

                if (leaveType.LEAV_TYPE_NAME == "CL" && availableBalance < viewModel.Duration)
                {
                    ModelState.AddModelError("", $"Not enough leave balance. Available: {availableBalance} days");
                    await PopulateViewBags();
                    return HandleResponse(View(viewModel), "Insufficient Balance");
                }

                string attachmentPath = null;

                if (viewModel.AttachmentFile != null && viewModel.AttachmentFile.Length > 0)
                {
                    try
                    {
                        var uploadsDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "leave_attachments");
                        Directory.CreateDirectory(uploadsDir);

                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(viewModel.AttachmentFile.FileName)}";
                        var filePath = Path.Combine(uploadsDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.AttachmentFile.CopyToAsync(stream);
                        }

                        attachmentPath = $"/uploads/leave_attachments/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("AttachmentFile", $"Failed to save file: {ex.Message}");
                        await PopulateViewBags();
                        return HandleResponse(View(viewModel), "File Upload Error");
                    }
                }

                var isAutoApproved = leaveType.LEAV_TYPE_NAME == "CL";
                var status = isAutoApproved ? "Approved" : "Pending";
                var approverName = isAutoApproved ? "Auto-Approved" : viewModel.ApproverName;

                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = viewModel.EmployeeId,
                    Employee = employee,
                    EmpeName = employee.EmpeName,
                    LeaveTypeId = viewModel.LeaveTypeId,
                    LeaveType = leaveType,
                    LeaveTypeName = leaveType.LEAV_TYPE_NAME,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    DurationType = viewModel.DurationType,
                    Duration = viewModel.Duration,
                    Reason = viewModel.Reason,
                    Status = status,
                    RequestedAt = DateTime.Now,
                    ApprovedAt = isAutoApproved ? DateTime.Now : (DateTime?)null,
                    ApprovedById = isAutoApproved ? null : viewModel.ApprovedById,
                    ApproverName = approverName,
                    UsedToDate = isAutoApproved ? currentUsedDays + viewModel.Duration : currentUsedDays,
                    AccrualBalance = isAutoApproved ? availableBalance - viewModel.Duration : availableBalance,
                    OriginalUsedToDate = currentUsedDays,
                    OriginalAccrualBalance = availableBalance,
                    LeaveBalance = leaveType.DEFAULT_DAY_PER_YEAR,
                    AttachmentFileName = viewModel.AttachmentFile?.FileName,
                    AttachmentContentType = viewModel.AttachmentFile?.ContentType,
                    AttachmentPath = attachmentPath,
                    AttachmentFileData = null
                };

                _context.LEAV_REQUESTS.Add(leaveRequest);

                if (leaveBalance != null)
                {
                    leaveBalance.Balance = (int)(leaveBalance.Balance - viewModel.Duration);
                }
                else
                {
                    var newBalance = new LeaveBalance
                    {
                        EmpeId = viewModel.EmployeeId,
                        LeaveTypeId = viewModel.LeaveTypeId,
                        Balance = (int)(leaveType.DEFAULT_DAY_PER_YEAR - viewModel.Duration),
                        Year = currentYear,
                        EmpeName = employee.EmpeName,
                        LeaveTypeName = leaveType.LEAV_TYPE_NAME,
                        CreatedDate = DateTime.Now
                    };
                    _context.LEAV_BALANCE.Add(newBalance);
                }
                await _context.SaveChangesAsync();

                if (!isAutoApproved && viewModel.ApprovedById.HasValue && viewModel.ApprovedById.Value > 0)
                {
                    var approver = await _context.EMPE_PROFILE
                                        .FirstOrDefaultAsync(e => e.EmpeId == viewModel.ApprovedById.Value);

                    if (approver != null && !string.IsNullOrEmpty(approver.Email))
                    {
                        string recipientEmail = approver.Email;
                        string subject = $"New Leave Request for Your Approval - {employee.EmpeName}";
                        string message = $"Dear {approver.EmpeName},<br/><br/>" +
                                         $"A new leave request has been submitted by <b>{employee.EmpeName}</b> ({employee.Email}).<br/><br/>" +
                                         $"<b>Leave Type:</b> {leaveType.LEAV_TYPE_NAME}<br/>" +
                                         $"<b>Start Date:</b> {viewModel.StartDate.ToString("MM/dd/yyyy")}<br/>" +
                                         $"<b>End Date:</b> {viewModel.EndDate.ToString("MM/dd/yyyy")}<br/>" +
                                         $"<b>Duration:</b> {viewModel.Duration} {viewModel.DurationType}<br/>" +
                                         $"<b>Reason:</b> {viewModel.Reason}<br/><br/>" +
                                         $"Please log in to the system to review and approve/reject this request.<br/><br/>" +
                                         $"Best regards,<br/>" +
                                         $"HR Products System";

                        string senderEmailFromProfile = employee.Email;
                        string senderNameFromProfile = employee.EmpeName; 

                        try
                        {
                             await _emailService.SendEmailAsync(recipientEmail, subject, message, senderEmailFromProfile, senderNameFromProfile);
                            TempData["SuccessMessage"] += " Email notification sent to approver!";
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to send leave request notification email to {recipientEmail}: {emailEx.Message}");
                            TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to approver failed.";
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Approver email not found or empty for EmpeId: {viewModel.ApprovedById}. Email notification skipped.");
                        TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Approver email not found for notification.";
                    }
                }
                else if (isAutoApproved)
                {
                    if (employee != null && !string.IsNullOrEmpty(employee.Email))
                    {
                        string recipientEmail = employee.Email;
                        string subject = $"Leave Request Auto-Approved - {leaveType.LEAV_TYPE_NAME}";
                        string message = $"Dear {employee.EmpeName},<br/><br/>" +
                                         $"Your leave request ({leaveType.LEAV_TYPE_NAME} from {viewModel.StartDate.ToString("MM/dd/yyyy")} to {viewModel.EndDate.ToString("MM/dd/yyyy")}) has been **automatically approved**.<br/><br/>" +
                                         $"Best regards,<br/>" +
                                         $"HR Products System";

                        await _emailService.SendEmailAsync(recipientEmail, subject, message, null, "HR Products System"); // No dynamic reply-to, uses system default
                        TempData["SuccessMessage"] += " Auto-approval email sent to employee!";
                    }
                }

                TempData["SuccessMessage"] = $"Leave request submitted successfully!";
                return HandleResponse(RedirectToAction("Create"), status);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while saving the leave request: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error saving leave request: {ex.Message}");
                await PopulateViewBags();
                return HandleResponse(View(viewModel), "Error");
            }
        }
        private IActionResult HandleResponse(IActionResult result, string status = null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (result is ViewResult viewResult)
                {
                    return Json(new
                    {
                        success = false,
                        message = string.Join("\n", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)),
                        status = status ?? "Error",
                        errors = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                    });
                }
                else if (result is RedirectToActionResult)
                {
                    return Json(new
                    {
                        success = true,
                        message = TempData["SuccessMessage"]?.ToString(),
                        status = status,
                        redirectUrl = Url.Action("Create")
                    });
                }
            }
            return result;
        }

        private async Task<byte[]> GetFileBytes(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var leaveRequest = await _context.LEAV_REQUESTS.FindAsync(id);

            if (leaveRequest == null || string.IsNullOrEmpty(leaveRequest.AttachmentPath))
            {
                return NotFound(); 
            }

            var webRootPath = _hostEnvironment.WebRootPath; 
            var filePath = Path.Combine(webRootPath, leaveRequest.AttachmentPath.TrimStart('/')); 

            Console.WriteLine($"Constructed file path: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"File does not exist at: {filePath}");
                return NotFound(); 
            }

            return PhysicalFile(filePath, leaveRequest.AttachmentContentType, leaveRequest.AttachmentFileName);
        }


        private async Task PopulateViewBags()
        {
            ViewBag.Employees = await _context.EMPE_PROFILE
                .Select(e => new { e.EmpeId, e.EmpeName })
                .ToListAsync();

            ViewBag.LeaveTypes = await _context.LEAV_TYPE
                .Select(lt => new { lt.LEAV_TYPE_ID, lt.LEAV_TYPE_NAME, lt.DESCRIPTION })
                .ToListAsync();

            ViewBag.DurationTypes = new List<string> { "Full-Day", "AM", "PM" };
        }

        [HttpGet]
        public async Task<JsonResult> CheckHolidays(DateTime startDate, DateTime endDate)
        {
            var holidays = await _context.HOLIDAYS
                .Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate)
                .Select(h => new { h.HolidayDate })
                .ToListAsync();

            return Json(holidays);
        }

        [HttpGet]
        public async Task<JsonResult> GetApprover(int leaveTypeId, int employeeId)
        {
            try
            {
                var leaveType = await _context.LEAV_TYPE
                    .FirstOrDefaultAsync(lt => lt.LEAV_TYPE_ID == leaveTypeId);

                if (leaveType == null)
                {
                    return Json(new { error = "Invalid leave type." });
                }
                decimal usedToDate = await _context.LEAV_REQUESTS
                    .Where(lr => lr.EmployeeId == employeeId &&
                           lr.LeaveTypeId == leaveTypeId &&
                           lr.Status == "Approved" &&
                           lr.StartDate.Year == DateTime.Now.Year)
                    .SumAsync(lr => (decimal?)lr.Duration) ?? 0;
                decimal balance = leaveType.DEFAULT_DAY_PER_YEAR;
                decimal accrualBalance = balance - usedToDate;
                if (leaveType.LEAV_TYPE_NAME == "CL")
                {

                    decimal approvedDuration = await _context.LEAV_REQUESTS
                        .Where(lr => lr.EmployeeId == employeeId
                                     && lr.LeaveTypeId == leaveTypeId
                                     && lr.Status == "Approved"
                                     && lr.StartDate.Year == DateTime.Now.Year)
                        .SumAsync(lr => (decimal?)lr.Duration) ?? 0;


                    var currentRequest = await _context.LEAV_REQUESTS
                        .FirstOrDefaultAsync(lr => lr.EmployeeId == employeeId
                                               && lr.LeaveTypeId == leaveTypeId
                                               && lr.Status != "Approved"
                                               && lr.StartDate.Year == DateTime.Now.Year);

                    if (currentRequest != null)
                    {

                        decimal totalUsed = approvedDuration + currentRequest.Duration;


                        currentRequest.UsedToDate = totalUsed;
                        currentRequest.AccrualBalance = leaveType.DEFAULT_DAY_PER_YEAR - totalUsed;
                        currentRequest.Status = "Approved";
                        currentRequest.ApprovedAt = DateTime.Now;


                        Console.WriteLine($"Updating UsedToDate to: {totalUsed}");
                        Console.WriteLine($"Updating AccrualBalance to: {leaveType.DEFAULT_DAY_PER_YEAR - totalUsed}");

                        await _context.SaveChangesAsync();

                        return Json(new
                        {
                            approverId = -1,
                            approverName = "Auto-Approved",
                            balance = leaveType.DEFAULT_DAY_PER_YEAR,
                            usedToDate = totalUsed,
                            accrualBalance = leaveType.DEFAULT_DAY_PER_YEAR - totalUsed,
                            isAutoApproved = true
                        });
                    }


                    return Json(new
                    {
                        approverId = -1,
                        approverName = "Auto-Approved",
                        balance = leaveType.DEFAULT_DAY_PER_YEAR,
                        usedToDate = approvedDuration,
                        accrualBalance = leaveType.DEFAULT_DAY_PER_YEAR - approvedDuration,
                        isAutoApproved = true
                    });
                }


                EmployeeProfile approver = null;
                string approverJobTitle = "";

                switch (leaveType.LEAV_TYPE_NAME)
                {
                    case "AL":
                        approverJobTitle = "Admin";
                        approver = await _context.EMPE_PROFILE
                            .FirstOrDefaultAsync(e => e.JobTitle == "Admin" && e.EmpeId != employeeId);
                        break;

                    case "HL":
                        approverJobTitle = "HR-Admin";
                        approver = await _context.EMPE_PROFILE
                            .FirstOrDefaultAsync(e => e.JobTitle == "HR-Admin" && e.EmpeId != employeeId);
                        break;

                    default:
                        approverJobTitle = "Admin";
                        approver = await _context.EMPE_PROFILE
                            .FirstOrDefaultAsync(e => (e.JobTitle == "Admin" || e.JobTitle == "HR-Admin")
                                                  && e.EmpeId != employeeId);
                        break;
                }


                if (approver == null)
                {
                    approver = await _context.EMPE_PROFILE
                        .FirstOrDefaultAsync(e => e.JobTitle == "Admin" && e.EmpeId != employeeId);
                }

                return Json(new
                {
                    approverId = approver?.EmpeId ?? 0,
                    approverName = approver?.EmpeName ?? $"No {approverJobTitle} available",
                    balance = balance,
                    usedToDate = usedToDate,
                    accrualBalance = accrualBalance,
                    isAutoApproved = false,
                    canApprove = accrualBalance > 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while processing your request" });
            }
        }



        private async Task<decimal> GetUsedToDate(int employeeId, int leaveTypeId)
        {
            return await _context.LEAV_REQUESTS
                .Where(lr => lr.EmployeeId == employeeId &&
                             lr.LeaveTypeId == leaveTypeId &&
                             lr.Status == "Approved")
                .SumAsync(lr => lr.Duration);
        }

        private decimal CalculateDuration(DateTime startDate, DateTime endDate, string durationType)
        {
            switch (durationType)
            {
                case "Full-Day":
                    return (decimal)(endDate - startDate).TotalDays + 1;
                case "AM":
                case "PM":
                    return 0.5m;
                default:
                    throw new ArgumentException("Invalid duration type");
            }
        }


        [HttpGet]
        public async Task<IActionResult> History(int? SelectedEmployeeId, int? SelectedLeaveTypeId)
        {
            bool isRegularUser = !User.IsInRole("Admin") && !User.IsInRole("HR-Admin");
            var currentUser = GetCurrentUser(); 

            IQueryable<LeaveRequest> query = _context.LEAV_REQUESTS
                .Include(lr => lr.Employee) 
                .Include(lr => lr.LeaveType)
                .AsQueryable();

            if (isRegularUser)
            {
                if (currentUser != null)
                {
                    query = query.Where(lr => lr.EmployeeId == currentUser.EmpeId);
                    SelectedEmployeeId = currentUser.EmpeId; 
                }
                else
                {
                    TempData["ErrorMessage"] = "Your employee profile could not be found. Please contact support.";
                    query = query.Where(lr => false); 
                }
            }
            else
            {
                if (SelectedEmployeeId.HasValue && SelectedEmployeeId > 0)
                {
                    query = query.Where(lr => lr.EmployeeId == SelectedEmployeeId.Value);
                }
            }

            if (SelectedLeaveTypeId.HasValue && SelectedLeaveTypeId > 0)
            {
                query = query.Where(lr => lr.LeaveTypeId == SelectedLeaveTypeId.Value);
            }

            var viewModel = new LeaveHistoryViewModel
            {
                LeaveRequests = await query
                    .OrderByDescending(lr => lr.StartDate)
                    .ToListAsync(),

                Employees = new SelectList(
                    await _context.EMPE_PROFILE
                        .OrderBy(e => e.EmpeName)
                        .ToListAsync(),
                    "EmpeId",
                    "EmpeName"),

                LeaveTypes = new SelectList(
                    await _context.LEAV_TYPE
                        .OrderBy(lt => lt.LEAV_TYPE_NAME)
                        .ToListAsync(),
                    "LEAV_TYPE_ID",
                    "LEAV_TYPE_NAME"),

                SelectedEmployeeId = SelectedEmployeeId,
                SelectedLeaveTypeId = SelectedLeaveTypeId
            };

            ViewBag.IsRegularUser = isRegularUser;

            return View(viewModel);
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


        [HttpGet]
        public async Task<IActionResult> Withdraw(int id)
        {
            var leaveRequest = await _context.LEAV_REQUESTS
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (leaveRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be withdrawn.";
                return RedirectToAction("History");
            }

            leaveRequest.Status = "Withdraw";
            leaveRequest.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Leave request successfully withdraw.";
            return RedirectToAction("History");
        }

        [HttpPost]
        public IActionResult SubmitLeave(LeaveRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Leave request submitted successfully!";
                return RedirectToAction("Create");
            }

            return View(model);
        }
    }
}
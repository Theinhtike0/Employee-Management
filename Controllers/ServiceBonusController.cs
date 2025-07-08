using HR_Products.Data;
using HR_Products.Models.Entitites;
using HR_Products.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HR_Products.Services;

namespace HR_Products.Controllers
{
    public class ServiceBonusController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ServiceBonusController> _logger;
        private readonly IEmailService _emailService;

        public ServiceBonusController(AppDbContext context, ILogger<ServiceBonusController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
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

        private (int Age, int ServiceYears) CalculateEmployeeYears(EmployeeProfile employee)
        {
            var today = DateTime.Today;
            int age = 0, serviceYears = 0;

            if (employee.DateOfBirth.HasValue)
            {
                age = today.Year - employee.DateOfBirth.Value.Year;
                if (employee.DateOfBirth.Value.Date > today.AddYears(-age)) age--;
            }

            if (employee.JoinDate != default) // Assuming JoinDate is DateTime
            {
                serviceYears = today.Year - employee.JoinDate.Year;
                if (employee.JoinDate.Date > today.AddYears(-serviceYears)) serviceYears--;
            }
            return (age, serviceYears);
        }

        private async Task<decimal> GetLastMonthBasicSalaryAsync(int employeeId)
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            var salary = await _context.PAYROLLS
                .Where(p => p.EmpeId == employeeId &&
                            p.PayDate.Month == lastMonth.Month &&
                            p.PayDate.Year == lastMonth.Year)
                .OrderByDescending(p => p.PayDate)
                .Select(p => p.BasicSalary)
                .FirstOrDefaultAsync();
            return salary;
        }

        private decimal CalculateBonus(int years, decimal salary)
        {
            return years < 5 ? years * salary : years * salary * 1.5m;
        }


        [HttpGet]
        public async Task<IActionResult> RequestBonus()
        {
            var employee = GetCurrentUser();

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee profile not found for the current user. Please log in.";
                return RedirectToAction("Login", "Account");
            }

            var (_, serviceYears) = CalculateEmployeeYears(employee);
            var basicSalary = await GetLastMonthBasicSalaryAsync(employee.EmpeId);

            
            string approverNameForDisplay = "HR Department Head"; 
            int? approverIdForDisplay = null;

            
            var hrManager = await _context.EMPE_PROFILE
                                        .FirstOrDefaultAsync(e => e.JobTitle == "Admin"); 
            if (hrManager != null)
            {
                approverNameForDisplay = hrManager.EmpeName;
                approverIdForDisplay = hrManager.EmpeId;
            }
            else
            {
                _logger.LogWarning("No HR Manager (JobTitle='Admin') found for anticipated approver. Using default placeholder.");
            }


            var model = new ServiceBonusViewModel
            {
                EmpeId = employee.EmpeId,
                EmpeName = employee.EmpeName,
                ServiceYears = serviceYears,
                BasicSalary = basicSalary,
                CalculatedBonus = CalculateBonus(serviceYears, basicSalary),
                IsEligible = serviceYears >= 1,
                LastDate = null, 
                ApprovedById = approverIdForDisplay,
                ApproverName = approverNameForDisplay 
            };

            
            var lastPreviousRequest = await _context.SERVICE_BONUS
                .Where(r => r.EmpeId == employee.EmpeId && r.Status != "Rejected")
                .OrderByDescending(r => r.RequestDate)
                .Select(r => (DateTime?)r.RequestDate)
                .FirstOrDefaultAsync();
            ViewBag.LastPreviousRequestDate = lastPreviousRequest;


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(ServiceBonusViewModel model)
        {
            EmployeeProfile employee = null;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                employee = GetCurrentUser(); 

                if (employee == null || employee.EmpeId != model.EmpeId)
                {
                    _logger.LogError("Security Alert: Employee mismatch for ID {ModelEmpeId} during bonus request submission by {UserName}.", model.EmpeId, User.Identity?.Name);
                    TempData["ErrorMessage"] = "Unauthorized request. Please log in as the correct employee.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Login", "Account");
                }

                var (_, serviceYears) = CalculateEmployeeYears(employee);
                var basicSalary = await GetLastMonthBasicSalaryAsync(employee.EmpeId);
                var calculatedBonus = CalculateBonus(serviceYears, basicSalary);
                bool isEligible = serviceYears >= 1;

                model.EmpeName = employee.EmpeName;
                model.ServiceYears = serviceYears;
                model.BasicSalary = basicSalary;
                model.CalculatedBonus = calculatedBonus;
                model.IsEligible = isEligible;

                EmployeeProfile hrManager = await _context.EMPE_PROFILE.FirstOrDefaultAsync(e => e.JobTitle == "Admin");

                string approverNameForDisplay = "HR Department Head";
                int? approverIdForDisplay = null;
                if (hrManager != null)
                {
                    approverNameForDisplay = hrManager.EmpeName;
                    approverIdForDisplay = hrManager.EmpeId;
                }
                model.ApproverName = approverNameForDisplay;
                model.ApprovedById = approverIdForDisplay;


                if (!model.IsEligible)
                {
                    ModelState.AddModelError("", "You are not eligible to submit a service bonus request based on service years.");
                }

                if (!model.LastDate.HasValue)
                {
                    ModelState.AddModelError("LastDate", "The 'Last Date (Your Last Day)' is required.");
                }
                else if (model.LastDate.Value.Date < DateTime.Today.Date)
                {
                    ModelState.AddModelError("LastDate", "The 'Last Date' cannot be in the past.");
                }

                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please correct the errors in the form.";
                    var lastPreviousRequest = await _context.SERVICE_BONUS
                        .Where(r => r.EmpeId == employee.EmpeId && r.Status != "Rejected")
                        .OrderByDescending(r => r.RequestDate)
                        .Select(r => (DateTime?)r.RequestDate)
                        .FirstOrDefaultAsync();
                    ViewBag.LastPreviousRequestDate = lastPreviousRequest;

                    await transaction.RollbackAsync();
                    return View("RequestBonus", model);
                }

                var newRequest = new ServiceBonusRequest
                {
                    EmpeId = model.EmpeId,
                    EmpeName = model.EmpeName,
                    ServiceYears = model.ServiceYears,
                    BasicSalary = model.BasicSalary,
                    BonusAmount = model.CalculatedBonus,
                    RequestDate = DateTime.UtcNow,
                    Status = "Pending",
                    LastDate = model.LastDate.Value,
                    ApprovedById = model.ApprovedById,
                    ApproverName = model.ApproverName
                };

                _context.SERVICE_BONUS.Add(newRequest);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Service bonus request submitted successfully!";
                _logger.LogInformation("Service bonus request submitted successfully for EmpeId: {EmpeId}, RequestId: {RequestId}", model.EmpeId, newRequest.RequestId);

                string lastDateFormatted = newRequest.LastDate.HasValue ? newRequest.LastDate.Value.ToString("MM/dd/yyyy") : "N/A";

                if (hrManager != null && !string.IsNullOrEmpty(hrManager.Email))
                {
                    string recipientEmail = hrManager.Email;
                    string subject = $"New Service Bonus Request from {employee.EmpeName}";
                    string message = $"Dear {hrManager.EmpeName},<br/><br/>" +
                                     $"A new service bonus request has been submitted by <b>{employee.EmpeName}</b>.<br/><br/>" +
                                     $"<b>Employee ID:</b> {employee.EmpeId}<br/>" +
                                     $"<b>Service Years:</b> {newRequest.ServiceYears}<br/>" +
                                     $"<b>Basic Salary:</b> {newRequest.BasicSalary.ToString("C")}<br/>" +
                                     $"<b>Calculated Bonus:</b> {newRequest.BonusAmount.ToString("C")}<br/>" +
                                     $"<b>Requested Last Day:</b> {lastDateFormatted}<br/><br/>" + // Use the safely formatted date
                                     $"Please log in to the HR Products system to review and approve/reject this request.<br/><br/>" +
                                     $"Best regards,<br/>" +
                                     $"The HR Products System";

                    string senderEmailFromProfile = employee.Email;
                    string senderNameFromProfile = employee.EmpeName;

                    string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                    string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                    try
                    {
                        await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                        TempData["SuccessMessage"] += " Email notification sent to approver!";
                        _logger.LogInformation("Service bonus request email sent to approver {ApproverEmail} from applicant {ApplicantEmail}", recipientEmail, actualSenderEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send service bonus request email to approver {RecipientEmail}.", recipientEmail);
                        TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to approver failed.";
                    }
                }
                else
                {
                    _logger.LogWarning("HR Manager email not found for service bonus request from employee {EmployeeName}. Email notification skipped.", employee.EmpeName);
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "HR Manager email not found for notification.";
                }

                return RedirectToAction("RequestBonus", "ServiceBonus");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error submitting service bonus request for employee {EmpeId}.", model.EmpeId);
                TempData["ErrorMessage"] = "An error occurred while submitting your request. Please try again.";

                if (employee != null)
                {
                    var lastPreviousRequest = await _context.SERVICE_BONUS
                        .Where(r => r.EmpeId == employee.EmpeId && r.Status != "Rejected")
                        .OrderByDescending(r => r.RequestDate)
                        .Select(r => (DateTime?)r.RequestDate)
                        .FirstOrDefaultAsync();
                    ViewBag.LastPreviousRequestDate = lastPreviousRequest;
                }
                return View("RequestBonus", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR-Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.SERVICE_BONUS.FindAsync(id);

                if (request == null)
                {
                    _logger.LogWarning("ApproveRequest: Service bonus request with ID {RequestId} not found.", id);
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToAction(nameof(ManageRequests));
                }

                if (request.Status != "Pending")
                {
                    _logger.LogWarning("ApproveRequest: Service bonus request with ID {RequestId} is not pending (current status: {Status}).", id, request.Status);
                    TempData["WarningMessage"] = "Only pending requests can be approved.";
                    return RedirectToAction(nameof(ManageRequests));
                }

                var approverEmployee = GetCurrentUser(); 
                if (approverEmployee == null)
                {
                    _logger.LogError("ApproveRequest: Approver details not found for current user {UserName}. Cannot approve request {RequestId}.", User.Identity?.Name, id);
                    TempData["ErrorMessage"] = "Approver details not found. Cannot approve.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Login", "Account");
                }

                request.Status = "Approved";
                request.ApprovalDate = DateTime.UtcNow;
                request.ApprovedById = approverEmployee.EmpeId;
                request.ApproverName = approverEmployee.EmpeName;

                var applicantEmployee = await _context.EMPE_PROFILE.FindAsync(request.EmpeId);
                if (applicantEmployee == null)
                {
                    _logger.LogError("ApproveRequest: Applicant's EmployeeProfile not found for EmpeId {EmpeId} for request {RequestId}.", request.EmpeId, id);
                    TempData["ErrorMessage"] = $"Approved request, but applicant's profile not found. Cannot send email.";
                   
                }

                if (request.LastDate.HasValue)
                {
                    if (applicantEmployee != null) 
                    {
                        applicantEmployee.TerminateDate = request.LastDate.Value;
                        _context.EMPE_PROFILE.Update(applicantEmployee);
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Approved request for employee ID {request.EmpeId}, but employee profile not found to update termination date.";
                    }
                }
                else
                {
                    TempData["WarningMessage"] = $"Request approved, but 'Last Date' was not provided, so employee termination date was not updated.";
                }

                _context.SERVICE_BONUS.Update(request); 
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); 

                TempData["SuccessMessage"] = "Service bonus request approved and employee termination date updated!";
                
                if (applicantEmployee != null && !string.IsNullOrEmpty(applicantEmployee.Email))
                {
                    string recipientEmail = applicantEmployee.Email;
                    string subject = $"Your Service Bonus Request Has Been Approved";
                    string message = $"Dear {applicantEmployee.EmpeName},<br/><br/>" +
                                     $"We are pleased to inform you that your service bonus request has been **APPROVED**.<br/><br/>" +
                                     $"<b>Request ID:</b> {request.RequestId}<br/>" +
                                     $"<b>Approved Amount:</b> {request.BonusAmount.ToString("C")}<br/>" +
                                     $"<b>Approved Date:</b> {request.ApprovalDate?.ToString("MM/dd/yyyy") ?? "N/A"}<br/>" +
                                     (request.LastDate.HasValue ? $"<b>Your Last Day of Employment:</b> {request.LastDate.Value.ToString("MM/dd/yyyy")}<br/><br/>" : "<br/>") +
                                     $"Further details regarding the payout will be communicated by the Finance Department.<br/><br/>" +
                                     $"Best regards,<br/>" +
                                     $"The HR Products Team";

                    string senderEmailFromProfile = approverEmployee.Email; 
                    string senderNameFromProfile = approverEmployee.EmpeName; 

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

                return RedirectToAction(nameof(ManageRequests));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); 
                TempData["ErrorMessage"] = $"Failed to approve service bonus request {id}: {ex.Message}";
                return RedirectToAction(nameof(ManageRequests));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR-Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(); 
            try
            {
                var request = await _context.SERVICE_BONUS.FindAsync(id);
                if (request == null)
                {
                    _logger.LogWarning("RejectRequest: Service bonus request with ID {RequestId} not found.", id);
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToAction(nameof(ManageRequests));
                }

                if (request.Status != "Pending")
                {
                    _logger.LogWarning("RejectRequest: Service bonus request with ID {RequestId} is not pending (current status: {Status}).", id, request.Status);
                    TempData["WarningMessage"] = "Only pending requests can be rejected.";
                    return RedirectToAction(nameof(ManageRequests));
                }

                var approverEmployee = GetCurrentUser(); 
                if (approverEmployee == null)
                {
                    _logger.LogError("RejectRequest: Approver details not found for current user {UserName}. Cannot reject request {RequestId}.", User.Identity?.Name, id);
                    TempData["ErrorMessage"] = "Approver details not found. Cannot reject.";
                    await transaction.RollbackAsync(); 
                    return RedirectToAction("Login", "Account");
                }

                var applicantEmployee = await _context.EMPE_PROFILE.FindAsync(request.EmpeId);
                if (applicantEmployee == null)
                {
                    _logger.LogError("RejectRequest: Applicant's EmployeeProfile not found for EmpeId {EmpeId} for request {RequestId}. Cannot send email.", request.EmpeId, id);
                    TempData["ErrorMessage"] = $"Rejected request, but applicant's profile not found. Cannot send email.";
                    
                }


                request.Status = "Rejected";
                request.ApprovalDate = DateTime.UtcNow; 
                request.ApprovedById = approverEmployee.EmpeId;
                request.ApproverName = approverEmployee.EmpeName;

                _context.SERVICE_BONUS.Update(request);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); 

                TempData["InfoMessage"] = "Service bonus request rejected.";
                _logger.LogInformation("Service bonus request ID {RequestId} rejected successfully by {ApproverName}.", id, approverEmployee.EmpeName);

                if (applicantEmployee != null && !string.IsNullOrEmpty(applicantEmployee.Email))
                {
                    string recipientEmail = applicantEmployee.Email;
                    string subject = $"Your Service Bonus Request Has Been Rejected";
                    string message = $"Dear {applicantEmployee.EmpeName},<br/><br/>" +
                                     $"We regret to inform you that your service bonus request has been **REJECTED**.<br/><br/>" +
                                     $"<b>Request ID:</b> {request.RequestId}<br/>" +
                                     $"<b>Rejected Date:</b> {request.ApprovalDate?.ToString("MM/dd/yyyy") ?? "N/A"}<br/>" + // Using ApprovalDate as rejection date
                                     $"<b>Requested Bonus Amount:</b> {request.BonusAmount.ToString("C")}<br/><br/>" +
                                     $"Please contact the HR Department for further details or clarification.<br/><br/>" +
                                     $"Best regards,<br/>" +
                                     $"The HR Products Team";

                    string senderEmailFromProfile = approverEmployee.Email;
                    string senderNameFromProfile = approverEmployee.EmpeName;

                    string actualSenderEmail = !string.IsNullOrEmpty(senderEmailFromProfile) ? senderEmailFromProfile : "no-reply@yourcompany.com";
                    string actualSenderName = !string.IsNullOrEmpty(senderNameFromProfile) ? senderNameFromProfile : "HR Products System";

                    try
                    {
                        await _emailService.SendEmailAsync(recipientEmail, subject, message, actualSenderEmail, actualSenderName);
                        TempData["InfoMessage"] += " Email notification sent to employee!"; // Use InfoMessage for consistency
                        _logger.LogInformation("Service bonus rejection email sent to employee {RecipientEmail} from approver {ApproverEmail}.", recipientEmail, actualSenderEmail);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send service bonus rejection email to employee {RecipientEmail}.", recipientEmail);
                        TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Email notification to employee failed.";
                    }
                }
                else
                {
                    _logger.LogWarning("Employee email not found for service bonus request ID {RequestId} after rejection. Email notification skipped.", id);
                    TempData["WarningMessage"] = TempData["WarningMessage"] + (string.IsNullOrEmpty(TempData["WarningMessage"] as string) ? "" : " ") + "Employee email not found for rejection notification.";
                }

                return RedirectToAction(nameof(ManageRequests));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); 
                _logger.LogError(ex, "Error rejecting service bonus request ID {RequestId}.", id);
                TempData["ErrorMessage"] = $"Failed to reject service bonus request {id}: {ex.Message}";
                return RedirectToAction(nameof(ManageRequests));
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR-Admin")]
        public async Task<IActionResult> ManageRequests()
        {
            var requests = await _context.SERVICE_BONUS
                .Include(r => r.Employee)
                .Include(r => r.Approver)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }
    }
}
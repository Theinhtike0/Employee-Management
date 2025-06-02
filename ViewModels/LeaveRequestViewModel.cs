using System.ComponentModel.DataAnnotations;
using HR_Products.Attributes;
using Microsoft.AspNetCore.Http; 

namespace HR_Products.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        public string EmpeName { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public int LeaveTypeId { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [Display(Name = "Duration Type")]
        public string DurationType { get; set; } = "Full-Day";

        [Display(Name = "Duration (Days)")]
        public decimal Duration { get; set; }

        public decimal LeaveBalance { get; set; }

        [Display(Name = "Used To Date")]
        public decimal UsedToDate { get; set; }

        [Display(Name = "Accrual Balance")]
        public decimal AccrualBalance { get; set; }

        [Required]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [Display(Name = "Approver")]
        public string? ApproverName { get; set; }

        public int? ApprovedById { get; set; }

        public string? Status { get; set; } = "Pending";

        [Display(Name = "Attachment File")]
        [RequiredIfLeaveType("HL", "SL", ErrorMessage = "Attachment file is required for HL/SL leave types")]
        public IFormFile? AttachmentFile { get; set; }

        public string? AttachmentPath { get; set; } 
        public string? AttachmentFileName { get; set; } 
        public string? AttachmentContentType { get; set; } 
    }
}
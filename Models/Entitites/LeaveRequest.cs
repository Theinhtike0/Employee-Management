using HR_Products.Models.Entitites;

namespace HR_Products.Models.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public EmployeeProfile Employee { get; set; }

        public string EmpeName { get; set; }
        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }
        public string LeaveTypeName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DurationType { get; set; }
        public decimal Duration { get; set; }

        public decimal LeaveBalance { get; set; }

        public string Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }

        // Changed to nullable
        public int? ApprovedById { get; set; }  // Changed from int to int?
        public EmployeeProfile? Approver { get; set; }  // Made nullable
        public string? ApproverName { get; set; }  // Made nullable

        public decimal UsedToDate { get; set; }
        public decimal AccrualBalance { get; set; }

        public decimal OriginalUsedToDate { get; set; }
        public decimal OriginalAccrualBalance { get; set; }

        public string? AttachmentPath { get; set; }
        public string? AttachmentFileName { get; set; }
        public byte[]? AttachmentFileData { get; set; }
        public string? AttachmentContentType { get; set; }
    }
}
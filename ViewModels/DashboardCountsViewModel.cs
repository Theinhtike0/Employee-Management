using HR_Products.Models.Entities;

namespace HR_Products.ViewModels
{
    public class DashboardCountsViewModel
    {
        public int LeaveRequestCount { get; set; }
        public int PayrollTransactionCount { get; set; }
        public int PensionCount { get; set; }
        public int AttendanceCount { get; set; }
        public List<LeaveViewModel> PendingLeaves { get; set; }

        public List<PensionViewModel> PendingPension { get; set; }
    }

    public class LeaveViewModel
    {
        public string EmployeeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime RequestedAt { get; set; }

        public decimal Duration { get; set; }
        public string Status { get; set; }
    }


    public class PensionViewModel
    {
        public string EmployeeName { get; set; }

        public string Department { get; set; }

        public string Position { get; set; }

        public int ServiceYears { get; set; }

        public int Age { get; set; }

        public String  Reason { get; set; }

        public string Status { get; set; }
    }
}
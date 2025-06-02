using HR_Products.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_Products.ViewModels
{
    public class LeaveHistoryViewModel
    {
        public int? SelectedEmployeeId { get; set; }
        public int? SelectedLeaveTypeId { get; set; }
        public List<LeaveRequest> LeaveRequests { get; set; }
        public SelectList Employees { get; set; }
        public SelectList LeaveTypes { get; set; }

        public string CurrentUserName { get; set; }
        
    }
}

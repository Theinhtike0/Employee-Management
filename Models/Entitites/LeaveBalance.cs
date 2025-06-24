using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HR_Products.Models.Entitites
{
    public class LeaveBalance
    {
        public int Id { get; set; }
        public int EmpeId { get; set; }
        public int LeaveTypeId { get; set; }
        public int Balance { get; set; }

        public int Year { get; set; }
        public string EmpeName { get; set; }   
        public string LeaveTypeName { get; set; }
        public DateTime CreatedDate { get; set; } 
        
        public EmployeeProfile Employee { get; set; }
        public LeaveType LeaveType { get; set; }
    }


}

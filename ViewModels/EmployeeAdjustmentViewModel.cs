using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class EmployeeAdjustmentViewModel
    {
        public int EmpeId { get; set; }
        public string ProfilePic { get; set; }
        public string EmpeName { get; set; }
        public string Email { get; set; }

        [Display(Name = "Join Date")] 
        public DateTime JoinDate { get; set; }

        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Age")] 
        public int? Age { get; set; } 


        [Display(Name = "Service Year")]
        public int ServiceYear { get; set; } 

        [Display(Name = "Salary")]
        public decimal? LatestNetPay { get; set; } 

        
    }
}

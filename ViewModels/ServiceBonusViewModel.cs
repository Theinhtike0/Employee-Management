using System;
using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class ServiceBonusViewModel
    {
        public int EmpeId { get; set; }

        [Display(Name = "Employee Name")]
        public string EmpeName { get; set; }

        [Display(Name = "Service Years")]
        public int ServiceYears { get; set; }

        [Display(Name = "Basic Salary")]
        [DataType(DataType.Currency)]
        public decimal BasicSalary { get; set; }

        [Display(Name = "Calculated Bonus")]
        [DataType(DataType.Currency)]
        public decimal CalculatedBonus { get; set; }

        [Display(Name = "Eligibility Status")]
        public bool IsEligible { get; set; }

        [Display(Name = "Last Date (Your Last Day)")] 
        [DataType(DataType.Date)]
        public DateTime? LastDate { get; set; }

        [Display(Name = "Approver ID")]
        public int? ApprovedById { get; set; } 

        [Display(Name = "Approver Name")]
        public string? ApproverName { get; set; } 
    }
}
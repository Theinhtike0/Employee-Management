
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class ReportViewModel
    {
        [Display(Name = "Employee Name")]
        [Required(ErrorMessage = "Please select an employee.")]
        public int SelectedEmployeeId { get; set; }
        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        [Display(Name = "Year")]
        [Required(ErrorMessage = "Please select a year.")]
        public int SelectedYear { get; set; }
        public List<SelectListItem> Years { get; set; } = new List<SelectListItem>();

        [Display(Name = "Month")]
        [Required(ErrorMessage = "Please select a month.")]
        public int SelectedMonth { get; set; }
        public List<SelectListItem> Months { get; set; } = new List<SelectListItem>();
        public bool IsEmployeeSelectionDisabled { get; set; } = false;
        public string CurrentUserName { get; set; } 

        public ReportViewModel()
        {
            SelectedYear = DateTime.Now.Year;
            SelectedMonth = DateTime.Now.Month;
        }
    }
}
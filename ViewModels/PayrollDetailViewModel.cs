using System;
using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class PayrollDetailViewModel
    {
        public int PayrollId { get; set; }
        public int EmpeId { get; set; }

        [Display(Name = "Employee Name")]
        public string EmpeName { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Position")]
        public string Position { get; set; }

        [Display(Name = "Basic Salary")]
        [DataType(DataType.Currency)]
        public decimal BasicSalary { get; set; }

        [Display(Name = "Allowance")]
        [DataType(DataType.Currency)]
        public decimal Allowance { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Income Tax")]
        [DataType(DataType.Currency)]
        public decimal Tax { get; set; }

        [Display(Name = "Other Deductions")]
        [DataType(DataType.Currency)]
        public decimal Deductions { get; set; }

        [Display(Name = "Gross Pay")]
        [DataType(DataType.Currency)]
        public decimal GrossPay { get; set; }

        [Display(Name = "Net Pay")]
        [DataType(DataType.Currency)]
        public decimal NetPay { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime FrDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        [Display(Name = "Paid Date")]
        [DataType(DataType.Date)]
        public DateTime PayDate { get; set; }

        [Display(Name = "Medical Leave Deduction (ML)")]
        [DataType(DataType.Currency)]
        public decimal MedicalLeaveDeduction { get; set; }
        [Display(Name = "Unpaid Leave Deduction (UPL)")]
        [DataType(DataType.Currency)]
        public decimal UnpaidLeaveDeduction { get; set; }
        [Display(Name = "Service Year Leave Deduction (SER-L)")]
        [DataType(DataType.Currency)]
        public decimal ServiceLeaveDeduction { get; set; }
    }
}
